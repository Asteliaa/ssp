"""Сборка отчета по ЛР8 на основе шаблона отчета ЛР6 (титульный лист, стили, колонтитулы)."""
import copy
import re
import sys
from pathlib import Path

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Pt, Mm, Emu

sys.path.insert(0, str(Path(__file__).parent))
import content as C
import shots

HERE = Path(__file__).parent
IMG = HERE / 'img'
IMG.mkdir(exist_ok=True)
TEMPLATE = Path(r'D:\Unik\SSP\work\lab-6-parallel\report\Отчет_ЛР6.docx')
import os
OUT = HERE / os.environ.get('OUTNAME', 'Отчет_ЛР8.docx')
NB = '\u00a0'
LOGS = C.LOGS

# ---------------------------------------------------------------- изображения
def read_log(name):
    return shots.clean(Path(LOGS / name).read_text(encoding='utf-8'))


def strip_time(lines):
    out = []
    for l in lines:
        if re.match(r'^(real|user|sys)\t', l):
            continue
        out.append(l)
    return out


main = read_log('main_raw.txt')
idx = {k: next(i for i, l in enumerate(main) if k in l) for k in
       ('ЗАДАНИЕ A', 'ЗАДАНИЕ B', 'ЗАДАНИЕ C', 'ЗАДАНИЕ D')}
sections = {
    'a': main[:idx['ЗАДАНИЕ B']],
    'b': main[idx['ЗАДАНИЕ B']:idx['ЗАДАНИЕ C']],
    'c': main[idx['ЗАДАНИЕ C']:idx['ЗАДАНИЕ D']],
    'd': strip_time(main[idx['ЗАДАНИЕ D']:]),
}
MAXL = 34
IMGSPEC = {
    'cli_info': read_log('cli_info.txt'),
    'cli_codellama': strip_time(read_log('cli_codellama.txt')),
    'cli_deepseek': strip_time(read_log('cli_deepseek.txt')),
    'simple': read_log('simple_raw.txt'),
    'main_a': sections['a'], 'main_b': sections['b'],
    'main_c': sections['c'], 'main_d': sections['d'],
}
SHOT_INFO = {}
for name, lines in IMGSPEC.items():
    # пустые ведущие строки и пустой ввод убираем
    while lines and not lines[0].strip():
        lines = lines[1:]
    wl = shots.wrap(lines)
    while wl and not wl[-1].strip():
        wl.pop()
    n_full = len(wl)
    n = shots.render(lines, IMG / f'{name}.png', max_lines=MAXL)
    SHOT_INFO[name] = (n_full, n)

# ---------------------------------------------------------------- документ
doc = docx.Document(str(TEMPLATE))
body = doc.element.body
children = list(body)
# титульный лист: элементы 0..43, остальное (кроме sectPr) удаляем
for el in children[44:-1]:
    body.remove(el)

# номер работы на титульном листе
for p in doc.paragraphs:
    if p.text.startswith('по лабораторной работе'):
        for r in p.runs:
            r.text = ''
        p.runs[0].text = 'по лабораторной работе №8'
        break

LATIN = re.compile(r'(?<![A-Za-z0-9А-Яа-я])[A-Za-z0-9_.#+/:\-]*[A-Za-z][A-Za-z0-9_.#+/:\-]*')


def split_runs(text):
    """Разбить текст на фрагменты (текст, курсив)."""
    parts = []
    pos = 0
    for m in LATIN.finditer(text):
        s, e = m.span()
        tok = m.group(0)
        trail = ''
        while tok and tok[-1] in '.:-/':
            trail = tok[-1] + trail
            tok = tok[:-1]
        e = s + len(tok)
        if s > pos:
            parts.append((text[pos:s], False))
        parts.append((tok, True))
        pos = e
    if pos < len(text):
        parts.append((text[pos:], False))
    return parts


def add_runs(par, text, size=None, force_italic=None):
    text = text.replace('{NB}', NB)
    for frag, ital in split_runs(text):
        if not frag:
            continue
        r = par.add_run(frag)
        if ital if force_italic is None else force_italic:
            r.italic = True
        if size:
            r.font.size = Pt(size)
    return par


# нумерация объектов
fig_no, tab_no = {}, {}
for b in C.BLOCKS:
    if b[0] == 'fig':
        fig_no[b[1]] = len(fig_no) + 1
    elif b[0] == 'table':
        tab_no[b[1]] = len(tab_no) + 1


def refs(text):
    text = re.sub(r'\{fig:(\w+)\}', lambda m: str(fig_no[m.group(1)]), text)
    text = re.sub(r'\{tab:(\w+)\}', lambda m: str(tab_no[m.group(1)]), text)
    return text


def set_pf(par, first=None, line=None, align=None, keep_next=False, page_break=False):
    pf = par.paragraph_format
    if first is not None:
        pf.first_line_indent = first
    if align is not None:
        par.alignment = align
    if keep_next:
        pf.keep_with_next = True
    if page_break:
        pf.page_break_before = True


def heading(text, level, center=False, page_break=False):
    par = doc.add_paragraph(style='Heading %d' % level)
    r = par.add_run(text)
    r.font.name = 'Times New Roman'
    rpr = r._r.get_or_add_rPr()
    rf = rpr.find(qn('w:rFonts'))
    for a in ('w:ascii', 'w:eastAsia', 'w:hAnsi', 'w:cs'):
        rf.set(qn(a), 'Times New Roman')
    if center:
        par.alignment = WD_ALIGN_PARAGRAPH.CENTER
        par.paragraph_format.first_line_indent = 0
    if page_break:
        par.paragraph_format.page_break_before = True
    return par


def add_numpr(par, num_id):
    ppr = par._p.get_or_add_pPr()
    numpr = ppr.get_or_add_numPr()
    numpr.get_or_add_ilvl().val = 0
    numpr.get_or_add_numId().val = num_id


def make_toc():
    t = doc.add_paragraph(style='ReportTitle')
    t.add_run('СОДЕРЖАНИЕ')
    t.paragraph_format.page_break_before = True
    par = doc.add_paragraph()
    par.paragraph_format.first_line_indent = 0
    for kind, txt in (('begin', None), ('instr', 'TOC \\o "1-2" \\h \\z \\u'), ('separate', None),
                      ('text', 'Обновите поле (F9)'), ('end', None)):
        r = par.add_run()
        if kind == 'instr':
            it = OxmlElement('w:instrText'); it.set(qn('xml:space'), 'preserve'); it.text = txt
            r._r.append(it)
        elif kind == 'text':
            r.text = txt
        else:
            fc = OxmlElement('w:fldChar'); fc.set(qn('w:fldCharType'), kind)
            r._r.append(fc)


def table_style_id():
    for s in doc.styles:
        if s.style_id == 'aff0':
            return s
    return None


def add_table(caption, header, rows, widths_cm):
    cap = doc.add_paragraph()
    set_pf(cap, first=0, align=WD_ALIGN_PARAGRAPH.LEFT, keep_next=True)
    add_runs(cap, caption)
    tbl = doc.add_table(rows=1 + len(rows), cols=len(header))
    st = table_style_id()
    if st is not None:
        tbl.style = st
    tbl.autofit = False
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    total = sum(widths_cm)
    twips = [round(w / total * 9354) for w in widths_cm]
    twips[-1] += 9354 - sum(twips)
    tblPr = tbl._tbl.tblPr
    tw_el = tblPr.find(qn('w:tblW'))
    tw_el.set(qn('w:w'), '9354'); tw_el.set(qn('w:type'), 'dxa')
    grid = tbl._tbl.tblGrid
    for gc, tw in zip(grid.findall(qn('w:gridCol')), twips):
        gc.set(qn('w:w'), str(tw))
    for ri, row in enumerate([header] + rows):
        tr = tbl.rows[ri]
        trPr = tr._tr.get_or_add_trPr()
        cs = OxmlElement('w:cantSplit'); trPr.append(cs)
        if ri == 0:
            th = OxmlElement('w:tblHeader'); trPr.append(th)
        for ci, val in enumerate(row):
            cell = tr.cells[ci]
            cell.width = Emu(int(twips[ci] * 635))
            tcPr = cell._tc.get_or_add_tcPr()
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            par = cell.paragraphs[0]
            pf = par.paragraph_format
            pf.first_line_indent = 0
            pf.space_before = Pt(0); pf.space_after = Pt(0)
            pf.line_spacing = 1.0
            par.alignment = WD_ALIGN_PARAGRAPH.CENTER
            if ri < len(rows) and ri < 0:
                pf.keep_with_next = True
            add_runs(par, val, size=12)
    # после таблицы пустых абзацев не создаем


def add_figure(key, fname, caption):
    path = IMG / fname
    from PIL import Image
    w, h = Image.open(path).size
    width_mm = min(130, w * 0.2646)
    par = doc.add_paragraph()
    set_pf(par, first=0, align=WD_ALIGN_PARAGRAPH.CENTER, keep_next=True)
    par.paragraph_format.line_spacing = 1.0
    par.add_run().add_picture(str(path), width=Mm(width_mm))
    cap = doc.add_paragraph(style='ReportCaption')
    n_full, n_shown = SHOT_INFO[Path(fname).stem]
    suffix = ' (фрагмент)' if n_full > n_shown else ''
    add_runs(cap, f'Рисунок {fig_no[key]} – {caption}{suffix}')


make_toc()
first_h1 = True
for b in C.BLOCKS:
    kind = b[0]
    if kind == 'h1':
        heading(b[1], 1, page_break=first_h1)
        first_h1 = False
    elif kind == 'h1c':
        heading(b[1], 1, center=True, page_break=False)
    elif kind == 'h2':
        heading(b[1], 2)
    elif kind == 'p':
        par = doc.add_paragraph()
        add_runs(par, refs(b[1]))
    elif kind == 'ul':
        for item in b[1]:
            par = doc.add_paragraph()
            set_pf(par, first=0, align=WD_ALIGN_PARAGRAPH.LEFT)
            add_numpr(par, 10)
            add_runs(par, item)
    elif kind == 'ol':
        for item in b[1]:
            par = doc.add_paragraph()
            add_numpr(par, 11)
            add_runs(par, item)
    elif kind == 'fig':
        add_figure(b[1], b[2], b[3])
    elif kind == 'table':
        add_table(f'Таблица {tab_no[b[1]]} – {b[2]}', b[3], b[4], b[5])
    elif kind == 'app':
        h = heading('ПРИЛОЖЕНИЕ ' + b[1], 1, center=True, page_break=True)
        h.paragraph_format.keep_with_next = True
        p2 = doc.add_paragraph(); set_pf(p2, first=0, align=WD_ALIGN_PARAGRAPH.CENTER, keep_next=True)
        p2.add_run(f'({b[2]})')
        p3 = doc.add_paragraph(); set_pf(p3, first=0, align=WD_ALIGN_PARAGRAPH.CENTER, keep_next=True)
        r = p3.add_run(b[3]); r.bold = True
    elif kind == 'codefiles':
        for fname, path in b[1]:
            cap = doc.add_paragraph()
            set_pf(cap, first=0, align=WD_ALIGN_PARAGRAPH.LEFT, keep_next=True)
            add_runs(cap, f'Файл {fname}')
            for line in Path(path).read_text(encoding='utf-8').rstrip('\n').split('\n'):
                cp = doc.add_paragraph(style='ReportCode')
                r = cp.add_run(line if line else ' ')
                r.font.size = Pt(10)

doc.save(str(OUT))
print('saved', OUT, SHOT_INFO)
