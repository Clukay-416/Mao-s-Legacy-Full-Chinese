"""Create deterministic gzip-compressed MLD1 copy/literal binary deltas."""
import gzip, struct, pathlib, argparse

def make_delta(original: bytes, target: bytes) -> bytes:
    block = 32
    index = {}
    for offset in range(0, len(original) - block + 1, 16):
        key = original[offset:offset + block]
        offsets = index.setdefault(key, [])
        if len(offsets) < 4:
            offsets.append(offset)
    result = bytearray(b'MLD1' + struct.pack('<QQ', len(original), len(target)))
    cursor = literal = 0
    while cursor < len(target):
        best_offset = best_length = 0
        for offset in index.get(target[cursor:cursor + block], ()):
            length = block
            limit = min(len(original) - offset, len(target) - cursor)
            while length + 4096 <= limit and original[offset+length:offset+length+4096] == target[cursor+length:cursor+length+4096]:
                length += 4096
            while length < limit and original[offset+length] == target[cursor+length]:
                length += 1
            if length > best_length:
                best_offset, best_length = offset, length
        if best_length:
            if cursor > literal:
                chunk = target[literal:cursor]
                result.extend(b'\x02' + struct.pack('<I', len(chunk)) + chunk)
            result.extend(b'\x01' + struct.pack('<QI', best_offset, best_length))
            cursor += best_length
            literal = cursor
        else:
            cursor += 1
    if cursor > literal:
        chunk = target[literal:cursor]
        result.extend(b'\x02' + struct.pack('<I', len(chunk)) + chunk)
    result.extend(b'\x00')
    return gzip.compress(bytes(result), compresslevel=9, mtime=0)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('original', type=pathlib.Path)
    parser.add_argument('target', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(make_delta(args.original.read_bytes(), args.target.read_bytes()))
    print(args.output.name, args.output.stat().st_size)
