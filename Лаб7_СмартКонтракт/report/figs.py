"""Сборка рисунков отчета из скриншотов Remix IDE."""
import sys
from pathlib import Path
from PIL import Image

SRC = Path(sys.argv[1])            # папка со скриншотами Remix
IMG = Path(__file__).parent / 'img'
IMG.mkdir(exist_ok=True)
BG = (34, 35, 52)
GAP = 10


def load(name):
    return Image.open(SRC / f'{name}.png').convert('RGB')


def trim_right(im, pad=16):
    """Обрезать пустое поле справа (фон терминала однородный)."""
    px = im.load()
    w, h = im.size
    ref = px[w - 2, h // 2]
    last = 0
    for x in range(w):
        for y in range(0, h, 2):
            p = px[x, y]
            if sum(abs(a - b) for a, b in zip(p, ref)) > 60:
                last = x
                break
    return im.crop((0, 0, min(w, last + pad), h))


def error_block(name):
    """Из снимка терминала оставить первое сообщение об ошибке с причиной."""
    im = load(name)
    w, h = im.size
    px = im.load()
    ref = px[w - 2, h - 2]
    rows = [y for y in range(h) if any(sum(abs(a - b) for a, b in zip(px[x, y], ref)) > 90 for x in range(20, min(w, 700), 3))]
    # группы строк текста
    groups, cur = [], [rows[0]]
    for y in rows[1:]:
        if y - cur[-1] > 3:
            groups.append(cur); cur = [y]
        else:
            cur.append(y)
    groups.append(cur)
    # первая группа - пустая строка транзакции с кнопкой Debug, далее сообщение: берем 5 строк текста
    text = [g for g in groups if g[0] > 45][:5]
    top, bottom = text[0][0] - 8, text[-1][-1] + 8
    return trim_right(im.crop((0, top, w, bottom)))


def stack(images, cols=1):
    """Разместить изображения в сетке на общем фоне."""
    rows = [images[i:i + cols] for i in range(0, len(images), cols)]
    cw = max(im.size[0] for im in images)
    W = cols * cw + (cols + 1) * GAP
    H = sum(max(im.size[1] for im in r) for r in rows) + (len(rows) + 1) * GAP
    out = Image.new('RGB', (W, H), BG)
    y = GAP
    for r in rows:
        x = GAP
        for im in r:
            out.paste(im, (x, y))
            x += cw + GAP
        y += max(im.size[1] for im in r) + GAP
    return out


def crop_full(name, box):
    return load(name).crop(box)


def side(*ims):
    """Изображения в один ряд (выравнивание по верху)."""
    W = sum(im.size[0] for im in ims) + (len(ims) + 1) * GAP
    H = max(im.size[1] for im in ims) + 2 * GAP
    out = Image.new('RGB', (W, H), BG)
    x = GAP
    for im in ims:
        out.paste(im, (x, GAP)); x += im.size[0] + GAP
    return out


def inner(im):
    """Снять внешнюю рамку GAP, чтобы не удваивать отступы при вложении."""
    w, h = im.size
    return im.crop((GAP, GAP, w - GAP, h - GAP))


FIGS = {
    'compile': lambda: side(crop_full('fig_compile_files', (50, 40, 370, 455)),
                            crop_full('fig_compile', (50, 40, 1000, 520))),
    'deploy': lambda: stack([inner(side(crop_full('fig_deploy_form', (50, 40, 450, 730)),
                                        crop_full('fig_deployed', (50, 67, 450, 647)))),
                             crop_full('fig_deployed', (470, 772, 1300, 810))]),
    'a_main': lambda: side(inner(stack([load('a_univ'), load('a_get1')])),
                           inner(stack([load('a_count3'), load('a_all3')]))),
    'a_upd': lambda: stack([load('a_get2'), load('a_count2')]),
    'a_err': lambda: stack([error_block('a_get1_deleted_log'), error_block('a_err_name'),
                            error_block('a_err_age'), error_block('a_err_admin')]),
    'b_major': lambda: stack([load('b_cs'), load('b_math'), load('b_bio'), load('b_cs_after'),
                              load('b_with'), load('b_dep')], cols=2),
    'c_main': lambda: side(inner(stack([load('c_name'), load('c_get1')])),
                           inner(stack([load('c_count3'), load('c_all')]))),
    'c_search': lambda: stack([load('c_avail1'), load('c_author'), load('c_avail2'), load('c_count2'),
                               load('c_get3')], cols=2),
    'c_err': lambda: stack([error_block('c_err_deleted'), error_block('c_err_title'),
                            error_block('c_err_year'), error_block('c_err_admin')]),
    'd_emp': lambda: side(inner(stack([load('d_name'), load('d_get1')])),
                          inner(stack([load('d_get1_raised'), load('d_payroll2')]))),
    'd_agg': lambda: stack([load('d_count3'), load('d_payroll1'), load('d_avg1'),
                            load('d_pos'), load('d_exp'), load('d_count2')], cols=2),
    'd_all': lambda: load('d_all'),
    'd_err': lambda: stack([error_block('d_err_fired'), error_block('d_err_salary'),
                            error_block('d_err_percent'), error_block('d_err_admin')]),
}

if __name__ == '__main__':
    for k, f in FIGS.items():
        im = f()
        im.save(IMG / f'{k}.png')
        print(k, im.size)
