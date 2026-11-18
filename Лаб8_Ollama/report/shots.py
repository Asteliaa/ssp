"""Отрисовка реального текстового вывода программ в изображения консоли."""
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

FONT = ImageFont.truetype(r'C:\Windows\Fonts\consola.ttf', 15)
SYM = ImageFont.truetype(r'C:\Windows\Fonts\seguisym.ttf', 14)
EMO = ImageFont.truetype(r'C:\Windows\Fonts\seguiemj.ttf', 14)
BG = (12, 12, 12)
FG = (204, 204, 204)
COLS = 72
CW = FONT.getlength('M')
LH = 19
PAD = 10


def clean(text):
    text = re.sub(r'\x1b\[[0-9;?]*[a-zA-Z]', '', text)
    text = text.replace('\r', '')
    # убрать строки-спиннеры ollama
    lines = [re.sub(r'^(?:[\u2800-\u28ff]\s*)+', '', l) for l in text.split('\n')]
    return lines


def wrap(lines, cols=COLS):
    out = []
    for l in lines:
        l = l.expandtabs(4)
        if not l:
            out.append('')
            continue
        indent = len(l) - len(l.lstrip(' '))
        while len(l) > cols:
            cut = l.rfind(' ', indent + 1, cols)
            if cut <= indent:
                cut = cols
            out.append(l[:cut].rstrip())
            l = ' ' * indent + l[cut:].lstrip()
        out.append(l)
    return out


def draw_line(d, x, y, line):
    for ch in line:
        o = ord(ch)
        if o in (0x2713, 0x2717, 0x2705, 0x274c):
            d.text((x, y), ch, font=SYM if o in (0x2713, 0x2717) else EMO,
                   fill=FG, embedded_color=(o not in (0x2713, 0x2717)))
        elif o > 0x1f000:
            d.text((x, y), ch, font=EMO, fill=FG, embedded_color=True)
        else:
            d.text((x, y), ch, font=FONT, fill=FG)
        x += CW * (2 if o > 0x1f000 or o in (0x2705, 0x274c) else 1)


def render(lines, out, max_lines=None):
    lines = wrap(lines)
    while lines and not lines[-1].strip():
        lines.pop()
    if max_lines and len(lines) > max_lines:
        lines = lines[:max_lines]
    w = int(CW * COLS + 2 * PAD)
    h = LH * len(lines) + 2 * PAD
    im = Image.new('RGB', (w, h), BG)
    d = ImageDraw.Draw(im)
    for i, l in enumerate(lines):
        draw_line(d, PAD, PAD + i * LH, l)
    im.save(out)
    return len(lines)
