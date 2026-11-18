"""После сохранения в Word: вернуть явное форматирование основного текста.

Word удаляет прямое форматирование, совпадающее со стилем Normal (точно 18 pt, отступ 12,5 мм,
по ширине), а check_stp.py проверяет именно прямое форматирование абзаца. Вид документа не меняется.
"""
import sys

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml.ns import qn
from docx.shared import Mm, Pt

path = sys.argv[1]
d = docx.Document(path)
started = False
n = 0
for p in d.paragraphs:
    st = p.style.name or ''
    if st.lower().startswith('heading') and p.text.strip().upper().startswith('1 '):
        started = True
    if not started or st != 'Normal' or not p.text.strip():
        continue
    ppr = p._p.pPr
    if ppr is not None and ppr.find(qn('w:numPr')) is not None:
        continue
    if p._p.findall('.//' + qn('w:drawing')) or p.alignment not in (None, WD_ALIGN_PARAGRAPH.JUSTIFY):
        continue
    pf = p.paragraph_format
    if pf.first_line_indent is not None and round(pf.first_line_indent.mm, 1) != 12.5:
        continue
    pf.first_line_indent = Mm(12.5)
    pf.space_before = Pt(0)
    pf.space_after = Pt(0)
    pf.line_spacing_rule = WD_LINE_SPACING.EXACTLY
    pf.line_spacing = Pt(18)
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    n += 1
d.save(path)
print('restored', n)
