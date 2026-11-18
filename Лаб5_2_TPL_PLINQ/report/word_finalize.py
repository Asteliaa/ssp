"""Обновляет содержание в Word, сохраняет DOCX и экспортирует PDF (как в отчетах ЛР2)."""
import pathlib
import time

import win32com.client

ROOT = pathlib.Path(__file__).resolve().parent
DOCX = ROOT / "Lab5_2_Report.docx"
PDF = ROOT / "Lab5_2_Report.pdf"


def log(msg):
    print(time.strftime("%H:%M:%S"), msg, flush=True)


word = win32com.client.DispatchEx("Word.Application")
word.Visible = False
word.DisplayAlerts = 0
try:
    doc = word.Documents.Open(str(DOCX))
    log("opened")
    for i in range(1, doc.TablesOfContents.Count + 1):
        doc.TablesOfContents(i).Update()
    doc.Repaginate()
    for i in range(1, doc.TablesOfContents.Count + 1):
        doc.TablesOfContents(i).UpdatePageNumbers()
    log("toc updated")
    doc.Save()
    log(f"saved, pages: {doc.ComputeStatistics(2)}")
    doc.ExportAsFixedFormat(str(PDF), 17, OpenAfterExport=False)
    log("pdf exported")
    doc.Close(0)
finally:
    word.Quit()
    log("word closed")
