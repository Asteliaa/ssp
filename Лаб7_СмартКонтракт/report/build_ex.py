"""Сборка отчета по ЛР7 по образцу отчета ЛР6 IoT (стили, титульный лист, оглавление, приложение)."""
import copy
import re
import sys
from pathlib import Path

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Mm, Pt
from PIL import Image

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, r'D:\MBP\ЛР1_Анализ_предметной_области\_tools')
import content as C  # noqa: E402
import strip_sol  # noqa: E402
from stp_polish import split_latin, clean_text  # noqa: E402

TEMPLATE = Path(r'D:\Unik\SSP\Лаб6_IoT\Лаб6_IoT_Отчет.docx')
OUT = HERE / 'Отчет_ЛР7.docx'
IMG = HERE / 'img'
NB = '\u00a0'

doc = docx.Document(str(TEMPLATE))
body = doc.element.body
els = list(body)
# 0..35: титульный лист и поле оглавления (последний абзац содержит конец поля)
first_h1 = next(i for i, el in enumerate(els)
                if el.tag == qn('w:p') and el.xpath('string(.)') == 'Цель работы')
tbl_tpl = copy.deepcopy(doc.tables[0]._tbl)
for el in els[first_h1:]:
    if el.tag != qn('w:sectPr'):
        body.remove(el)

for p in doc.paragraphs[:15]:
    if p.text.startswith('по лабораторной работе'):
        for r in p.runs[1:]:
            r.text = ''
        p.runs[0].text = 'по лабораторной работе №' + NB + '7'

H1_NAMES = {'1 ЦЕЛЬ РАБОТЫ': 'Цель работы', '2 ТЕОРЕТИЧЕСКИЕ СВЕДЕНИЯ': 'Теоретические сведения',
            '3 ВЫПОЛНЕНИЕ РАБОТЫ': 'Выполнение работы'}


def prep(text):
    """Ссылки на источник убраны (в образце нет списка источников), символы по профилю."""
    text = text.replace('{NB}', NB)
    text = re.sub(r'\s*\[1\]', '', text)
    return clean_text(text)


def add_text(par, text, italic_all=False):
    for frag, it in split_latin(prep(text)):
        if not frag:
            continue
        r = par.add_run(frag)
        if it or italic_all:
            r.italic = True
    return par


def para(style, text='', **kw):
    p = doc.add_paragraph(style=style)
    if text:
        add_text(p, text)
    for k, v in kw.items():
        setattr(p.paragraph_format, k, v)
    return p


fig_no, tab_no = {}, {}
for b in C.BLOCKS:
    if b[0] == 'fig':
        fig_no[b[1]] = len(fig_no) + 1
    elif b[0] == 'table':
        tab_no[b[1]] = len(tab_no) + 1


def refs(text):
    text = re.sub(r'\{fig:(\w+)\}', lambda m: str(fig_no[m.group(1)]), text)
    return re.sub(r'\{tab:(\w+)\}', lambda m: str(tab_no[m.group(1)]), text)


def add_table(no, caption, header, rows, widths):
    para('Caption Table', f'Таблица {no} – {caption}')
    tbl = copy.deepcopy(tbl_tpl)
    for tr in tbl.findall(qn('w:tr')):
        tbl.remove(tr)
    grid = tbl.find(qn('w:tblGrid'))
    for gc in grid.findall(qn('w:gridCol')):
        grid.remove(gc)
    total = sum(widths)
    tw = [round(w / total * 9355) for w in widths]
    tw[-1] += 9355 - sum(tw)
    for w in tw:
        gc = OxmlElement('w:gridCol'); gc.set(qn('w:w'), str(w)); grid.append(gc)
    for ri, row in enumerate([header] + rows):
        tr = OxmlElement('w:tr')
        trpr = OxmlElement('w:trPr')
        trpr.append(OxmlElement('w:cantSplit'))
        if ri == 0:
            trpr.append(OxmlElement('w:tblHeader'))
        jc = OxmlElement('w:jc'); jc.set(qn('w:val'), 'center'); trpr.append(jc)
        tr.append(trpr)
        for ci, val in enumerate(row):
            tc = OxmlElement('w:tc')
            tcpr = OxmlElement('w:tcPr')
            tcw = OxmlElement('w:tcW'); tcw.set(qn('w:w'), str(tw[ci])); tcw.set(qn('w:type'), 'dxa')
            va = OxmlElement('w:vAlign'); va.set(qn('w:val'), 'center')
            tcpr.append(tcw); tcpr.append(va); tc.append(tcpr)
            p = OxmlElement('w:p')
            ppr = OxmlElement('w:pPr')
            ps = OxmlElement('w:pStyle'); ps.set(qn('w:val'), 'TableText'); ppr.append(ps)
            if ri < len(rows):
                ppr.append(OxmlElement('w:keepNext'))
            p.append(ppr)
            tc.append(p)
            tr.append(tc)
            from docx.text.paragraph import Paragraph
            add_text(Paragraph(p, doc._body), val)
        tbl.append(tr)
    body.insert(len(body) - 1, tbl)
    return True


def add_figure(key, fname, caption):
    path = IMG / fname
    w, _ = Image.open(path).size
    width = min(165, w * C.SCALE.get(key, C.DEFAULT_SCALE))
    p = para('Figure Para')
    p.add_run().add_picture(str(path), width=Mm(width))
    para('Caption Fig', f'Рисунок {fig_no[key]} – {caption}')


def code(lines):
    for line in lines:
        if not line.strip():
            continue
        p = doc.add_paragraph(style='Code')
        ind = len(line) - len(line.lstrip(' '))
        if ind:
            p.add_run(' ' * ind)
        for frag, it in split_latin(line.lstrip(' ')):
            r = p.add_run(frag)
            if it:
                r.italic = True


after_table = False
skip_sources = False
for b in C.BLOCKS:
    kind = b[0]
    if skip_sources and kind == 'ol':
        skip_sources = False
        continue
    if kind == 'h1':
        para('Heading 1', H1_NAMES[b[1]])
    elif kind == 'h2':
        para('Heading 2', re.sub(r'^\d+\.\d+\s+', '', b[1]))
    elif kind == 'h1c':
        if b[1].startswith('СПИСОК'):
            skip_sources = True
            continue
        para('Heading Unnumbered', b[1].capitalize())
    elif kind == 'p':
        text = refs(b[1])
        if text.startswith('Работа выполнена в порядке, заданном'):
            continue  # в образце за заголовком раздела сразу следует подраздел
        text = text.replace('Текст функции приведен ниже.',
                            'Текст функции приведен в приложении А (подраздел А.3).')
        text = text.replace('а код контрактов, написанных в работе, приведен в приложении А',
                            'а код контрактов, разработанных в работе, приведен в приложении А')
        text = re.sub(r' Смарт-контракты применяются не только.*?информации( \[1\])?\.', '', text)
        p = para('Normal', text)
        if after_table:
            p.paragraph_format.space_before = Pt(18)
    elif kind == 'ul':
        for item in b[1]:
            para('List Dash', item)
    elif kind == 'fig':
        add_figure(b[1], b[2], b[3])
    elif kind == 'table':
        add_table(tab_no[b[1]], b[2], b[3], b[4], b[5])
    elif kind == 'code':
        continue  # код функции вынесен в приложение А.3
    elif kind in ('app', 'codefiles'):
        continue
    after_table = kind == 'table'

# Приложение А по образцу
p = para('Heading Unnumbered', 'Приложение А')
p.paragraph_format.space_after = Pt(0)
p = para('Normal', '(обязательное)')
p.alignment = WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.first_line_indent = Mm(0)
p = doc.add_paragraph(style='Normal')
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.first_line_indent = Mm(0)
p.paragraph_format.space_after = Pt(18)
for frag, it in split_latin('Код контрактов, разработанных в работе'):
    r = p.add_run(frag); r.bold = True; r.italic = True if it else None
para('Normal', 'В приложении приведен код контрактов, разработанных при выполнении заданий, без комментариев. '
               'Контракты StudentRegistry и ExtendedStudentRegistry совпадают с листингом методических указаний '
               'и не повторяются.')
for title, names in (('А.1 Контракты варианта 1: BookRegistry и ExtendedBookRegistry',
                      ('BookRegistry', 'ExtendedBookRegistry')),
                     ('А.2 Контракты варианта 2: EmployeeRegistry и наследник',
                      ('EmployeeRegistry', 'ExtendedEmployeeRegistry')),
                     ('А.3 Контракт StudentRegistryPlus для дополнительного задания',
                      ('StudentRegistryPlus',))):
    para('Heading Appendix', title)
    code(strip_sol.join_headers(strip_sol.contracts(names)))

doc.save(str(OUT))
print('saved', OUT, 'figs', len(fig_no), 'tables', len(tab_no))
