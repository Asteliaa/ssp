"""Обновление полей и оглавления в Word, сохранение docx и экспорт в PDF через Word COM."""
import os
import sys

import pythoncom
import win32com.client as wc

src = os.path.abspath(sys.argv[1])
dst = os.path.splitext(src)[0] + ".pdf"
pythoncom.CoInitialize()
w = wc.DispatchEx("Word.Application")
w.Visible = False
w.DisplayAlerts = 0
try:
    d = w.Documents.Open(src)
    d.Fields.Update()
    for t in d.TablesOfContents:
        t.Update()
    d.Save()
    d.ExportAsFixedFormat(dst, 17)
    print("pages:", d.ComputeStatistics(2))
    d.Close(False)
finally:
    w.Quit()
print(dst)
