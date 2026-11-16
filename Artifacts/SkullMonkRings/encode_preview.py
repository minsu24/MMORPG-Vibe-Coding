"""Package Unity-rendered PNG frames as a lossless looping APNG. No pixel changes."""
from pathlib import Path
import struct
import zlib

root = Path(__file__).parent
frames = sorted((root / 'frames').glob('*.png'))

def chunks(data):
    pos = 8
    while pos < len(data):
        n = struct.unpack('>I', data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        yield kind, data[pos + 8:pos + 8 + n]
        pos += 12 + n

def chunk(kind, data):
    return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)

first = list(chunks(frames[0].read_bytes()))
header = next(payload for kind, payload in first if kind == b'IHDR')
width, height = struct.unpack('>II', header[:8])
result = bytearray(b'\x89PNG\r\n\x1a\n')
result += chunk(b'IHDR', header)
result += chunk(b'acTL', struct.pack('>II', len(frames), 0))
sequence = 0
for i, frame in enumerate(frames):
    # Pause on the last frame before repeating; all other frames retain 60 FPS timing.
    delay_num, delay_den = (1, 30)
    result += chunk(b'fcTL', struct.pack('>IIIIIHHBB', sequence, width, height, 0, 0, delay_num, delay_den, 0, 0))
    sequence += 1
    for kind, payload in chunks(frame.read_bytes()):
        if kind != b'IDAT':
            continue
        if i == 0:
            result += chunk(b'IDAT', payload)
        else:
            result += chunk(b'fdAT', struct.pack('>I', sequence) + payload)
            sequence += 1
result += chunk(b'IEND', b'')
(root / 'ring-pattern-preview.png').write_bytes(result)
print(f'Encoded {len(frames)} lossless frames, {width}x{height}, {len(result)} bytes')
