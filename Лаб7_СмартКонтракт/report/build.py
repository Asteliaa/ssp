"""Сборка отчета по ЛР7 на основе шаблона отчета ЛР6 (титульный лист, стили, колонтитулы)."""
import re
import sys
from pathlib import Path

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Pt, Mm, Emu
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
import content as C
import strip_sol
sys.path.insert(0, r'D:\MBP\ЛР1_Анализ_предметной_области\_tools')
import stp_polish
from docx.enum.text import WD_LINE_SPACING

HERE = Path(__file__).parent
IMG = HERE / 'img'
TEMPLATE = Path(r'D:\Unik\SSP\work\lab-6-parallel\report\Отчет_ЛР6.docx')
OUT = HERE / 'Отчет_ЛР7.docx'
NB = '\u00a0'

doc = docx.Document(str(TEMPLATE))
body = doc.element.body
children = list(body)
# титульный лист: элементы 0..43, остальное (кроме sectPr) удаляем
for el in children[44:-1]:
    body.remove(el)

for p in doc.paragraphs:
    if p.text.startswith('по лабораторной работе'):
        for r in p.runs:
            r.text = ''
        p.runs[0].text = 'по лабораторной работе №7'
        break

LATIN = re.compile(r'(?<![A-Za-z0-9А-Яа-я])[A-Za-z0-9_.#+/:\-()&]*[A-Za-z][A-Za-z0-9_.#+/:\-()&]*')


def split_runs(text):
    parts = []
    pos = 0
    for m in LATIN.finditer(text):
        s, e = m.span()
        tok = m.group(0)
        # не захватывать завершающую пунктуацию и непарные скобки
        while tok and (tok[-1] in '.:-/' or (tok[-1] == ')' and tok.count('(') < tok.count(')'))):
            tok = tok[:-1]
        while tok and tok[0] == '(' and tok.count('(') > tok.count(')'):
            tok = tok[1:]
            s += 1
        e = s + len(tok)
        if s > pos:
            parts.append((text[pos:s], False))
        parts.append((tok, True))
        pos = e
    if pos < len(text):
        parts.append((text[pos:], False))
    return parts


def add_runs(par, text, size=None):
    text = text.replace('{NB}', NB)
    for frag, ital in split_runs(text):
        if not frag:
            continue
        r = par.add_run(frag)
        if ital:
            r.italic = True
        if size:
            r.font.size = Pt(size)
    return par


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


def set_pf(par, first=None, align=None, keep_next=False):
    pf = par.paragraph_format
    if first is not None:
        pf.first_line_indent = first
    if align is not None:
        par.alignment = align
    if keep_next:
        pf.keep_with_next = True


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


def table_style():
    for s in doc.styles:
        if s.style_id == 'aff0':
            return s
    return None


def add_table(caption, header, rows, widths_cm):
    cap = doc.add_paragraph()
    set_pf(cap, first=0, align=WD_ALIGN_PARAGRAPH.LEFT, keep_next=True)
    add_runs(cap, caption)
    tbl = doc.add_table(rows=1 + len(rows), cols=len(header))
    st = table_style()
    if st is not None:
        tbl.style = st
    tbl.autofit = False
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    total = sum(widths_cm)
    twips = [round(w / total * 9354) for w in widths_cm]
    twips[-1] += 9354 - sum(twips)
    tblPr = tbl._tbl.tblPr
    tw_el = tblPr.find(qn('w:tblW'))
    if tw_el is None:
        tw_el = OxmlElement('w:tblW'); tblPr.append(tw_el)
    tw_el.set(qn('w:w'), '9354'); tw_el.set(qn('w:type'), 'dxa')
    grid = tbl._tbl.tblGrid
    for gc, tw in zip(grid.findall(qn('w:gridCol')), twips):
        gc.set(qn('w:w'), str(tw))
    for ri, row in enumerate([header] + rows):
        tr = tbl.rows[ri]
        trPr = tr._tr.get_or_add_trPr()
        trPr.append(OxmlElement('w:cantSplit'))
        if ri == 0:
            trPr.append(OxmlElement('w:tblHeader'))
        for ci, val in enumerate(row):
            cell = tr.cells[ci]
            cell.width = Emu(int(twips[ci] * 635))
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            par = cell.paragraphs[0]
            pf = par.paragraph_format
            pf.first_line_indent = 0
            pf.space_before = Pt(0); pf.space_after = Pt(0)
            pf.line_spacing = 1.0
            par.alignment = WD_ALIGN_PARAGRAPH.CENTER
            if ri < len(rows):
                pf.keep_with_next = True  # таблица не разрывается между страницами
            add_runs(par, val, size=12)


def add_figure(key, fname, caption):
    path = IMG / fname
    w, h = Image.open(path).size
    width_mm = min(165, w * C.SCALE.get(key, C.DEFAULT_SCALE))
    par = doc.add_paragraph()
    set_pf(par, first=0, align=WD_ALIGN_PARAGRAPH.CENTER, keep_next=True)
    par.paragraph_format.line_spacing = 1.0
    par.add_run().add_picture(str(path), width=Mm(width_mm))
    cap = doc.add_paragraph(style='ReportCaption')
    add_runs(cap, f'Рисунок {fig_no[key]} – {caption}')


def body_par():
    """Абзац основного текста: 14 pt, интервал точно 18 pt, отступ 12,5 мм, по ширине."""
    par = doc.add_paragraph()
    pf = par.paragraph_format
    pf.first_line_indent = Mm(12.5)
    pf.left_indent = Mm(0)
    pf.space_before = Pt(0)
    pf.space_after = Pt(0)
    pf.line_spacing_rule = WD_LINE_SPACING.EXACTLY
    pf.line_spacing = Pt(18)
    par.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    return par


def code_lines(lines, size, keep=False, line_pt=None):
    """Строки кода: пустые строки не выводятся, латиница курсивом (профиль СТП)."""
    lines = [l for l in lines if l.strip()]
    for i, line in enumerate(lines):
        cp = doc.add_paragraph(style='ReportCode')
        pf = cp.paragraph_format
        pf.space_before = Pt(0)
        pf.space_after = Pt(0)
        if line_pt:
            pf.line_spacing_rule = WD_LINE_SPACING.EXACTLY
            pf.line_spacing = Pt(line_pt)
        ind = len(line) - len(line.lstrip(' '))
        r = cp.add_run(NB * ind + line.lstrip(' '))
        r.font.size = Pt(size)
        r.italic = True
        if keep and i < len(lines) - 1:
            pf.keep_with_next = True


make_toc()
first_h1 = True
for b in C.BLOCKS:
    kind = b[0]
    if kind == 'h1':
        heading(b[1], 1, page_break=first_h1)
        first_h1 = False
    elif kind == 'h1c':
        heading(b[1], 1, center=True)
    elif kind == 'h2':
        heading(b[1], 2)
    elif kind == 'p':
        add_runs(body_par(), refs(b[1]))
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
    elif kind == 'code':
        code_lines([w for l in b[1].split('\n') for w in strip_sol.wrap(l)], 12, keep=True)
    elif kind == 'app':
        h = heading('ПРИЛОЖЕНИЕ ' + b[1], 1, center=True, page_break=True)
        h.paragraph_format.keep_with_next = True
        p2 = doc.add_paragraph(); set_pf(p2, first=0, align=WD_ALIGN_PARAGRAPH.CENTER, keep_next=True)
        p2.add_run(f'({b[2]})')
        p3 = doc.add_paragraph(); set_pf(p3, first=0, align=WD_ALIGN_PARAGRAPH.CENTER, keep_next=True)
        r = p3.add_run(b[3]); r.bold = True
    elif kind == 'codefiles':
        for fname, path in b[1]:
            code_lines(strip_sol.student_code_wrapped(), 10, line_pt=10)

stp_polish.polish(doc)

# титульный лист умещается на одной странице (интервалы как в отчетах МБП/TableMind)
TITLE_SPACE = {'ОТЧЕТ': 108, 'Выполнил': 72, 'Проверила': 18, 'Минск': 90}
for par in doc.paragraphs[:20]:
    for key, pt in TITLE_SPACE.items():
        if par.text.strip().startswith(key):
            par.paragraph_format.space_before = Pt(pt)

# таблицы не разрываются: строки (кроме последней) держатся со следующей
for t in doc.tables:
    for row in t.rows[:-1]:
        for cell in row.cells:
            for cp in cell.paragraphs:
                cp.paragraph_format.keep_with_next = True
doc.save(str(OUT))
print('saved', OUT, 'figs', len(fig_no), 'tables', len(tab_no))
