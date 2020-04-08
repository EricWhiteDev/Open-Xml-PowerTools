﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace OpenXmlPowerTools
{
    internal class ReverseRevisionsInfo
    {
        public bool InInsert;
    }

    public class RevisionProcessor
    {
        public static WmlDocument RejectRevisions(WmlDocument document)
        {
            using (var streamDoc = new OpenXmlMemoryStreamDocument(document))
            {
                using (var doc = streamDoc.GetWordprocessingDocument())
                {
                    RejectRevisions(doc);
                }
                return streamDoc.GetModifiedWmlDocument();
            }
        }

        public static void RejectRevisions(WordprocessingDocument doc)
        {
            RejectRevisionsForPart(doc.MainDocumentPart);
            foreach (var part in doc.MainDocumentPart.HeaderParts)
            {
                RejectRevisionsForPart(part);
            }

            foreach (var part in doc.MainDocumentPart.FooterParts)
            {
                RejectRevisionsForPart(part);
            }

            if (doc.MainDocumentPart.EndnotesPart != null)
            {
                RejectRevisionsForPart(doc.MainDocumentPart.EndnotesPart);
            }

            if (doc.MainDocumentPart.FootnotesPart != null)
            {
                RejectRevisionsForPart(doc.MainDocumentPart.FootnotesPart);
            }

            if (doc.MainDocumentPart.StyleDefinitionsPart != null)
            {
                RejectRevisionsForStylesDefinitionPart(doc.MainDocumentPart.StyleDefinitionsPart);
            }

            ReverseRevisions(doc);
            AcceptRevisionsForPart(doc.MainDocumentPart);
            foreach (var part in doc.MainDocumentPart.HeaderParts)
            {
                AcceptRevisionsForPart(part);
            }

            foreach (var part in doc.MainDocumentPart.FooterParts)
            {
                AcceptRevisionsForPart(part);
            }

            if (doc.MainDocumentPart.EndnotesPart != null)
            {
                AcceptRevisionsForPart(doc.MainDocumentPart.EndnotesPart);
            }

            if (doc.MainDocumentPart.FootnotesPart != null)
            {
                AcceptRevisionsForPart(doc.MainDocumentPart.FootnotesPart);
            }

            if (doc.MainDocumentPart.StyleDefinitionsPart != null)
            {
                AcceptRevisionsForStylesDefinitionPart(doc.MainDocumentPart.StyleDefinitionsPart);
            }
        }

        // Reject revisions for those revisions that can't be rejected by inverting the sense of the revision, and then accepting.
        private static void RejectRevisionsForPart(OpenXmlPart part)
        {
            var xDoc = part.GetXDocument();
            var newRoot = RejectRevisionsForPartTransform(xDoc.Root);
            xDoc.Root.ReplaceWith(newRoot);
            part.PutXDocument();
        }

        private static object RejectRevisionsForPartTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                // Inserted Numbering Properties

                if (element.Name == W.numPr && element.Element(W.ins) != null)
                {
                    return null;
                }

                // Paragraph properties change

                if (element.Name == W.pPr &&
                    element.Element(W.pPrChange) != null)
                {
                    var pPr = element.Element(W.pPrChange).Element(W.pPr);
                    if (pPr == null)
                    {
                        pPr = new XElement(W.pPr);
                    }

                    var new_pPr = new XElement(pPr); // clone it
                    new_pPr.Add(RejectRevisionsForPartTransform(element.Element(W.rPr)));
                    return RejectRevisionsForPartTransform(new_pPr);
                }

                // Run properties change

                if (element.Name == W.rPr &&
                    element.Element(W.rPrChange) != null)
                {
                    var new_rPr = element.Element(W.rPrChange).Element(W.rPr);
                    return RejectRevisionsForPartTransform(new_rPr);
                }

                // Field code numbering change

                if (element.Name == W.numberingChange)
                {
                    return null;
                }

                // Change w:sectPr

                if (element.Name == W.sectPr &&
                    element.Element(W.sectPrChange) != null)
                {
                    var newSectPr = element.Element(W.sectPrChange).Element(W.sectPr);
                    return RejectRevisionsForPartTransform(newSectPr);
                }

                // tblGridChange

                if (element.Name == W.tblGrid &&
                    element.Element(W.tblGridChange) != null)
                {
                    var newTblGrid = element.Element(W.tblGridChange).Element(W.tblGrid);
                    return RejectRevisionsForPartTransform(newTblGrid);
                }

                // tcPrChange

                if (element.Name == W.tcPr &&
                    element.Element(W.tcPrChange) != null)
                {
                    var newTcPr = element.Element(W.tcPrChange).Element(W.tcPr);
                    return RejectRevisionsForPartTransform(newTcPr);
                }

                // trPrChange
                if (element.Name == W.trPr &&
                    element.Element(W.trPrChange) != null)
                {
                    var newTrPr = element.Element(W.trPrChange).Element(W.trPr);
                    return RejectRevisionsForPartTransform(newTrPr);
                }

                // tblPrExChange

                if (element.Name == W.tblPrEx &&
                    element.Element(W.tblPrExChange) != null)
                {
                    var newTblPrEx = element.Element(W.tblPrExChange).Element(W.tblPrEx);
                    return RejectRevisionsForPartTransform(newTblPrEx);
                }

                // tblPrChange

                if (element.Name == W.tblPr &&
                    element.Element(W.tblPrChange) != null)
                {
                    var newTrPr = element.Element(W.tblPrChange).Element(W.tblPr);
                    return RejectRevisionsForPartTransform(newTrPr);
                }

                // tblPrChange

                if (element.Name == W.cellDel ||
                    element.Name == W.cellMerge)
                {
                    return null;
                }

                if (element.Name == W.tc &&
                    element.Elements(W.tcPr).Elements(W.cellIns).Any())
                {
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => RejectRevisionsForPartTransform(n)));
            }
            return node;
        }

        private static void RejectRevisionsForStylesDefinitionPart(StyleDefinitionsPart stylesDefinitionsPart)
        {
            var xDoc = stylesDefinitionsPart.GetXDocument();
            var newRoot = RejectRevisionsForStylesTransform(xDoc.Root);
            xDoc.Root.ReplaceWith(newRoot);
            stylesDefinitionsPart.PutXDocument();
        }

        private static object RejectRevisionsForStylesTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.pPr &&
                    element.Element(W.pPrChange) != null)
                {
                    var new_pPr = element.Element(W.pPrChange).Element(W.pPr);
                    return RejectRevisionsForStylesTransform(new_pPr);
                }

                if (element.Name == W.rPr &&
                    element.Element(W.rPrChange) != null)
                {
                    var new_rPr = element.Element(W.rPrChange).Element(W.rPr);
                    return RejectRevisionsForStylesTransform(new_rPr);
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => RejectRevisionsForStylesTransform(n)));
            }
            return node;
        }

        private static void ReverseRevisions(WordprocessingDocument doc)
        {
            ReverseRevisionsForPart(doc.MainDocumentPart);
            foreach (var part in doc.MainDocumentPart.HeaderParts)
            {
                ReverseRevisionsForPart(part);
            }

            foreach (var part in doc.MainDocumentPart.FooterParts)
            {
                ReverseRevisionsForPart(part);
            }

            if (doc.MainDocumentPart.EndnotesPart != null)
            {
                ReverseRevisionsForPart(doc.MainDocumentPart.EndnotesPart);
            }

            if (doc.MainDocumentPart.FootnotesPart != null)
            {
                ReverseRevisionsForPart(doc.MainDocumentPart.FootnotesPart);
            }
        }

        private static void ReverseRevisionsForPart(OpenXmlPart part)
        {
            var xDoc = part.GetXDocument();
            var rri = new ReverseRevisionsInfo
            {
                InInsert = false
            };
            var newRoot = (XElement)ReverseRevisionsTransform(xDoc.Root, rri);
            newRoot = (XElement)RemoveRsidTransform(newRoot);
            xDoc.Root.ReplaceWith(newRoot);
            part.PutXDocument();
        }

        private static object RemoveRsidTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.rsid)
                {
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes().Where(a => a.Name != W.rsid &&
                        a.Name != W.rsidDel &&
                        a.Name != W.rsidP &&
                        a.Name != W.rsidR &&
                        a.Name != W.rsidRDefault &&
                        a.Name != W.rsidRPr &&
                        a.Name != W.rsidSect &&
                        a.Name != W.rsidTr),
                    element.Nodes().Select(n => RemoveRsidTransform(n)));
            }
            return node;
        }

        private static object MergeAdjacentTablesTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Element(W.tbl) != null)
                {
                    var grouped = element
                        .Elements()
                        .GroupAdjacent(e =>
                        {
                            if (e.Name != W.tbl)
                            {
                                return "";
                            }

                            var bidiVisual = e.Elements(W.tblPr).Elements(W.bidiVisual).FirstOrDefault();
                            var bidiVisString = bidiVisual == null ? "" : "|bidiVisual";
                            var key = "tbl" + bidiVisString;
                            return key;
                        });

                    var newContent = grouped
                        .Select(g =>
                        {
                            if (g.Key == "" || g.Count() == 1)
                            {
                                return (object)g;
                            }

                            var rolled = g
                                .Select(tbl =>
                                {
                                    var gridCols = tbl
                                        .Elements(W.tblGrid)
                                        .Elements(W.gridCol)
                                        .Attributes(W._w)
                                        .Select(a => (int)a)
                                        .Rollup(0, (s, i) => s + i);
                                    return gridCols;
                                })
                                .SelectMany(m => m)
                                .Distinct()
                                .OrderBy(w => w)
                                .ToArray();
                            var newTable = new XElement(W.tbl,
                                g.First().Elements(W.tblPr),
                                new XElement(W.tblGrid,
                                    rolled.Select((r, i) =>
                                    {
                                        int v;
                                        if (i == 0)
                                        {
                                            v = r;
                                        }
                                        else
                                        {
                                            v = r - rolled[i - 1];
                                        }

                                        return new XElement(W.gridCol,
                                            new XAttribute(W._w, v));
                                    })),
                                g.Select(tbl =>
                                {
                                    var fixedWidthsTbl = FixWidths(tbl);
                                    var newRows = fixedWidthsTbl.Elements(W.tr)
                                        .Select(tr =>
                                        {
                                            var newRow = new XElement(W.tr,
                                                tr.Attributes(),
                                                tr.Elements().Where(e => e.Name != W.tc),
                                                tr.Elements(W.tc).Select(tc =>
                                                {
                                                    var w = (int?)tc
                                                        .Elements(W.tcPr)
                                                        .Elements(W.tcW)
                                                        .Attributes(W._w)
                                                        .FirstOrDefault();
                                                    if (w == null)
                                                    {
                                                        return tc;
                                                    }

                                                    var cellsToLeft = tc
                                                        .Parent
                                                        .Elements(W.tc)
                                                        .TakeWhile(btc => btc != tc);
                                                    var widthToLeft = 0;
                                                    if (cellsToLeft.Any())
                                                    {
                                                        widthToLeft = cellsToLeft
                                                        .Elements(W.tcPr)
                                                        .Elements(W.tcW)
                                                        .Attributes(W._w)
                                                        .Select(wi => (int)wi)
                                                        .Sum();
                                                    }

                                                    var rolledPairs = new[] { new
                                                        {
                                                            GridValue = 0,
                                                            Index = 0,
                                                        }}
                                                        .Concat(
                                                            rolled
                                                            .Select((r, i) => new
                                                            {
                                                                GridValue = r,
                                                                Index = i + 1,
                                                            }));
                                                    var start = rolledPairs
                                                        .FirstOrDefault(t => t.GridValue >= widthToLeft);
                                                    if (start != null)
                                                    {
                                                        var gridsRequired = rolledPairs
                                                            .Skip(start.Index)
                                                            .TakeWhile(rp => rp.GridValue - start.GridValue < w)
                                                            .Count();
                                                        var tcPr = new XElement(W.tcPr,
                                                                tc.Elements(W.tcPr).Elements().Where(e => e.Name != W.gridSpan),
                                                                gridsRequired != 1 ?
                                                                    new XElement(W.gridSpan,
                                                                        new XAttribute(W.val, gridsRequired)) :
                                                                    null);
                                                        var orderedTcPr = new XElement(W.tcPr,
                                                            tcPr.Elements().OrderBy(e =>
                                                            {
                                                                if (Order_tcPr.ContainsKey(e.Name))
                                                                {
                                                                    return Order_tcPr[e.Name];
                                                                }

                                                                return 999;
                                                            }));
                                                        var newCell = new XElement(W.tc,
                                                            orderedTcPr,
                                                            tc.Elements().Where(e => e.Name != W.tcPr));
                                                        return newCell;
                                                    }
                                                    return tc;
                                                }));
                                            return newRow;
                                        });
                                    return newRows;
                                }));
                            return newTable;
                        });
                    return new XElement(element.Name,
                        element.Attributes(),
                        newContent);
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => MergeAdjacentTablesTransform(n)));
            }
            return node;
        }

        private static object ReverseRevisionsTransform(XNode node, ReverseRevisionsInfo rri)
        {
            var element = node as XElement;
            if (element != null)
            {
                var parent = element
                    .Ancestors()
                    .FirstOrDefault(a => a.Name != W.sdtContent && a.Name != W.sdt && a.Name != W.smartTag);

                // Deleted run

                if (element.Name == W.del &&
                    parent.Name == W.p)
                {
                    return new XElement(W.ins,
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Deleted paragraph mark

                if (element.Name == W.del &&
                    parent.Name == W.rPr &&
                    parent.Parent.Name == W.pPr)
                {
                    return new XElement(W.ins);
                }

                // Inserted paragraph mark

                if (element.Name == W.ins &&
                    parent.Name == W.rPr &&
                    parent.Parent.Name == W.pPr)
                {
                    return new XElement(W.del);
                }

                // Inserted run

                if (element.Name == W.ins &&
                    parent.Name == W.p)
                {
                    return new XElement(W.del,
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Deleted table row

                if (element.Name == W.del &&
                    parent.Name == W.trPr)
                {
                    return new XElement(W.ins);
                }

                // Inserted table row

                if (element.Name == W.ins &&
                    parent.Name == W.trPr)
                {
                    return new XElement(W.del);
                }

                // Deleted math control character

                if (element.Name == W.del &&
                    parent.Name == M.r)
                {
                    return new XElement(W.ins,
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Inserted math control character

                if (element.Name == W.ins &&
                    parent.Name == M.r)
                {
                    return new XElement(W.del,
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // moveFrom / moveTo

                if (element.Name == W.moveFrom)
                {
                    return new XElement(W.moveTo,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.moveFromRangeStart)
                {
                    return new XElement(W.moveToRangeStart,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.moveFromRangeEnd)
                {
                    return new XElement(W.moveToRangeEnd,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.moveTo)
                {
                    return new XElement(W.moveFrom,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.moveToRangeStart)
                {
                    return new XElement(W.moveFromRangeStart,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.moveToRangeEnd)
                {
                    return new XElement(W.moveFromRangeEnd,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Deleted content control

                if (element.Name == W.customXmlDelRangeStart)
                {
                    return new XElement(W.customXmlInsRangeStart,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.customXmlDelRangeEnd)
                {
                    return new XElement(W.customXmlInsRangeEnd,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Inserted content control

                if (element.Name == W.customXmlInsRangeStart)
                {
                    return new XElement(W.customXmlDelRangeStart,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.customXmlInsRangeEnd)
                {
                    return new XElement(W.customXmlDelRangeEnd,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Moved content control

                if (element.Name == W.customXmlMoveFromRangeStart)
                {
                    return new XElement(W.customXmlMoveToRangeStart,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.customXmlMoveFromRangeEnd)
                {
                    return new XElement(W.customXmlMoveToRangeEnd,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.customXmlMoveToRangeStart)
                {
                    return new XElement(W.customXmlMoveFromRangeStart,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }
                if (element.Name == W.customXmlMoveToRangeEnd)
                {
                    return new XElement(W.customXmlMoveFromRangeEnd,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Deleted field code
                if (element.Name == W.delInstrText)
                {
                    return new XElement(W.instrText,
                        element.Attributes(), // pulls in xml:space attribute
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Change inserted instrText element to w:delInstrText
                if (element.Name == W.instrText && rri.InInsert)
                {
                    return new XElement(W.delInstrText,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Change inserted text element to w:delText
                if (element.Name == W.t && rri.InInsert)
                {
                    return new XElement(W.delText,
                        element.Attributes(),
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Change w:delText to w:t
                if (element.Name == W.delText)
                {
                    return new XElement(W.t,
                        element.Attributes(), // pulls in xml:space attribute
                        element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
                }

                // Identity transform
                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => ReverseRevisionsTransform(n, rri)));
            }
            return node;
        }

        public static WmlDocument AcceptRevisions(WmlDocument document)
        {
            using (var streamDoc = new OpenXmlMemoryStreamDocument(document))
            {
                using (var doc = streamDoc.GetWordprocessingDocument())
                {
                    AcceptRevisions(doc);
                }
                return streamDoc.GetModifiedWmlDocument();
            }
        }

        public static void AcceptRevisions(WordprocessingDocument doc)
        {
            AcceptRevisionsForPart(doc.MainDocumentPart);
            foreach (var part in doc.MainDocumentPart.HeaderParts)
            {
                AcceptRevisionsForPart(part);
            }

            foreach (var part in doc.MainDocumentPart.FooterParts)
            {
                AcceptRevisionsForPart(part);
            }

            if (doc.MainDocumentPart.EndnotesPart != null)
            {
                AcceptRevisionsForPart(doc.MainDocumentPart.EndnotesPart);
            }

            if (doc.MainDocumentPart.FootnotesPart != null)
            {
                AcceptRevisionsForPart(doc.MainDocumentPart.FootnotesPart);
            }

            if (doc.MainDocumentPart.StyleDefinitionsPart != null)
            {
                AcceptRevisionsForStylesDefinitionPart(doc.MainDocumentPart.StyleDefinitionsPart);
            }
        }

        private static void AcceptRevisionsForStylesDefinitionPart(StyleDefinitionsPart stylesDefinitionsPart)
        {
            var xDoc = stylesDefinitionsPart.GetXDocument();
            var newRoot = AcceptRevisionsForStylesTransform(xDoc.Root);
            xDoc.Root.ReplaceWith(newRoot);
            stylesDefinitionsPart.PutXDocument();
        }

        private static object AcceptRevisionsForStylesTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.pPrChange || element.Name == W.rPrChange)
                {
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AcceptRevisionsForStylesTransform(n)));
            }
            return node;
        }

        public static void AcceptRevisionsForPart(OpenXmlPart part)
        {
            var documentElement = part.GetXDocument().Root;
            documentElement = (XElement)RemoveRsidTransform(documentElement);
            documentElement = (XElement)FixUpDeletedOrInsertedFieldCodesTransform(documentElement);
            var containsMoveFromMoveTo = documentElement.Descendants(W.moveFrom).Any();
            documentElement = (XElement)AcceptMoveFromMoveToTransform(documentElement);
            documentElement = AcceptMoveFromRanges(documentElement);
            // AcceptParagraphEndTagsInMoveFromTransform needs rewritten similar to AcceptDeletedAndMoveFromParagraphMarks
            documentElement = (XElement)AcceptParagraphEndTagsInMoveFromTransform(documentElement);
            documentElement = AcceptDeletedAndMovedFromContentControls(documentElement);
            documentElement = AcceptDeletedAndMoveFromParagraphMarks(documentElement);
            if (containsMoveFromMoveTo)
            {
                documentElement = (XElement)RemoveRowsLeftEmptyByMoveFrom(documentElement);
            }

            documentElement = (XElement)AcceptAllOtherRevisionsTransform(documentElement);
            documentElement = (XElement)AcceptDeletedCellsTransform(documentElement);
            documentElement = (XElement)MergeAdjacentTablesTransform(documentElement);
            documentElement = (XElement)AddEmptyParagraphToAnyEmptyCells(documentElement);
            documentElement.Descendants().Attributes().Where(a => a.Name == PT.UniqueId || a.Name == PT.RunIds).Remove();
            documentElement.Descendants(W.numPr).Where(np => !np.HasElements).Remove();
            var newXDoc = new XDocument(documentElement);
            part.PutXDocument(newXDoc);
        }

        // Note that AcceptRevisionsForElement is an incomplete implementation.  It is not possible to accept all varieties of revisions
        // for a single paragraph.  The paragraph may contain a marker for a deleted or inserted content control, as one example, of
        // which there are many.  This method accepts simple revisions, such as deleted or inserted text, which is the most common use
        // case.
        public static XElement AcceptRevisionsForElement(XElement element)
        {
            var rElement = element;
            rElement = (XElement)RemoveRsidTransform(rElement);
            rElement = (XElement)AcceptMoveFromMoveToTransform(rElement);
            rElement = (XElement)AcceptAllOtherRevisionsTransform(rElement);
            rElement.Descendants().Attributes().Where(a => a.Name == PT.UniqueId || a.Name == PT.RunIds).Remove();
            rElement.Descendants(W.numPr).Where(np => !np.HasElements).Remove();
            return rElement;
        }

        private static object FixUpDeletedOrInsertedFieldCodesTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.p)
                {
                    // 1 other
                    // 2 w:del/w:r/w:fldChar
                    // 3 w:ins/w:r/w:fldChar
                    // 4 w:instrText

                    // formulate new paragraph, looking for 4 that has 2 (or 3) before and after.  Then put in a w:del (or w:ins), transforming w:instrText to w:delInstrText if w:del.
                    // transform 1, 2, 3 as usual

                    var groupedParaContentsKey = element.Elements().Select(e =>
                    {
                        if (e.Name == W.del && e.Elements(W.r).Elements(W.fldChar).Any())
                        {
                            return 2;
                        }

                        if (e.Name == W.ins && e.Elements(W.r).Elements(W.fldChar).Any())
                        {
                            return 3;
                        }

                        if (e.Name == W.r && e.Element(W.instrText) != null)
                        {
                            return 4;
                        }

                        return 1;
                    });

                    var zipped = element.Elements().Zip(groupedParaContentsKey, (e, k) => new { Ele = e, Key = k });

                    var grouped = zipped.GroupAdjacent(z => z.Key).ToArray();

                    var gLen = grouped.Length;

                    var newParaContents = grouped
                        .Select((g, i) =>
                        {
                            if (g.Key == 1 || g.Key == 2 || g.Key == 3)
                            {
                                return (object)g.Select(gc => FixUpDeletedOrInsertedFieldCodesTransform(gc.Ele));
                            }

                            if (g.Key == 4)
                            {
                                if (i == 0 || i == gLen - 1)
                                {
                                    return g.Select(gc => FixUpDeletedOrInsertedFieldCodesTransform(gc.Ele));
                                }

                                if (grouped[i - 1].Key == 2 &&
                                    grouped[i + 1].Key == 2)
                                {
                                    return new XElement(W.del,
                                        g.Select(gc => TransformInstrTextToDelInstrText(gc.Ele)));
                                }
                                else if (grouped[i - 1].Key == 3 &&
                                    grouped[i + 1].Key == 3)
                                {
                                    return new XElement(W.ins,
                                        g.Select(gc => FixUpDeletedOrInsertedFieldCodesTransform(gc.Ele)));
                                }
                                else
                                {
                                    return g.Select(gc => FixUpDeletedOrInsertedFieldCodesTransform(gc.Ele));
                                }
                            }
                            throw new OpenXmlPowerToolsException("Internal error");
                        });

                    var newParagraph = new XElement(W.p,
                        element.Attributes(),
                        newParaContents);
                    return newParagraph;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => FixUpDeletedOrInsertedFieldCodesTransform(n)));
            }
            return node;
        }

        private static object TransformInstrTextToDelInstrText(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.instrText)
                {
                    return new XElement(W.delInstrText,
                        element.Attributes(),
                        element.Nodes());
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => TransformInstrTextToDelInstrText(n)));
            }
            return node;
        }

        private static object AddEmptyParagraphToAnyEmptyCells(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.tc && !element.Elements().Any(e => e.Name != W.tcPr))
                {
                    return new XElement(W.tc,
                        element.Attributes(),
                        element.Elements(),
                        new XElement(W.p));
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AddEmptyParagraphToAnyEmptyCells(n)));
            }
            return node;
        }

        private static readonly Dictionary<XName, int> Order_tcPr = new Dictionary<XName, int>
        {
            { W.cnfStyle, 10 },
            { W.tcW, 20 },
            { W.gridSpan, 30 },
            { W.hMerge, 40 },
            { W.vMerge, 50 },
            { W.tcBorders, 60 },
            { W.shd, 70 },
            { W.noWrap, 80 },
            { W.tcMar, 90 },
            { W.textDirection, 100 },
            { W.tcFitText, 110 },
            { W.vAlign, 120 },
            { W.hideMark, 130 },
            { W.headers, 140 },
        };

        private static XElement FixWidths(XElement tbl)
        {
            var newTbl = new XElement(tbl);
            var gridLines = tbl.Elements(W.tblGrid).Elements(W.gridCol).Attributes(W._w).Select(w => (int)w).ToArray();
            foreach (var tr in newTbl.Elements(W.tr))
            {
                var used = 0;
                var lastUsed = -1;
                foreach (var tc in tr.Elements(W.tc))
                {
                    var tcW = tc.Elements(W.tcPr).Elements(W.tcW).Attributes(W._w).FirstOrDefault();
                    if (tcW != null)
                    {
                        var gridSpan = (int?)tc.Elements(W.tcPr).Elements(W.gridSpan).Attributes(W.val).FirstOrDefault();

                        if (gridSpan == null)
                        {
                            gridSpan = 1;
                        }

                        var z = Math.Min(gridLines.Length - 1, lastUsed + (int)gridSpan);
                        var w = gridLines.Where((g, i) => i > lastUsed && i <= z).Sum();
                        tcW.Value = w.ToString();

                        lastUsed += (int)gridSpan;
                        used += (int)gridSpan;
                    }
                }
            }
            return newTbl;
        }

        private static object AcceptMoveFromMoveToTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.moveTo)
                {
                    return element.Nodes().Select(n => AcceptMoveFromMoveToTransform(n));
                }

                if (element.Name == W.moveFrom)
                {
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AcceptMoveFromMoveToTransform(n)));
            }
            return node;
        }

        private static XElement AcceptMoveFromRanges(XElement document)
        {
            // The following lists contain the elements that are between start/end elements.
            var startElementTagsInMoveFromRange = new List<XElement>();
            var endElementTagsInMoveFromRange = new List<XElement>();

            // Following are the elements that *may* be in a range that has both start and end
            // elements.
            var potentialDeletedElements =
                new Dictionary<string, PotentialInRangeElements>();

            foreach (var tag in DescendantAndSelfTags(document))
            {
                if (tag.Element.Name == W.moveFromRangeStart)
                {
                    var id = tag.Element.Attribute(W.id).Value;
                    potentialDeletedElements.Add(id, new PotentialInRangeElements());
                    continue;
                }
                if (tag.Element.Name == W.moveFromRangeEnd)
                {
                    var id = tag.Element.Attribute(W.id).Value;
                    if (potentialDeletedElements.ContainsKey(id))
                    {
                        startElementTagsInMoveFromRange.AddRange(
                            potentialDeletedElements[id].PotentialStartElementTagsInRange);
                        endElementTagsInMoveFromRange.AddRange(
                            potentialDeletedElements[id].PotentialEndElementTagsInRange);
                        potentialDeletedElements.Remove(id);
                    }
                    continue;
                }
                if (potentialDeletedElements.Count > 0)
                {
                    if (tag.TagType == TagTypeEnum.Element &&
                        (tag.Element.Name != W.moveFromRangeStart &&
                         tag.Element.Name != W.moveFromRangeEnd))
                    {
                        foreach (var id in potentialDeletedElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                        }

                        continue;
                    }
                    if (tag.TagType == TagTypeEnum.EmptyElement &&
                        (tag.Element.Name != W.moveFromRangeStart &&
                         tag.Element.Name != W.moveFromRangeEnd))
                    {
                        foreach (var id in potentialDeletedElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }
                        continue;
                    }
                    if (tag.TagType == TagTypeEnum.EndElement &&
                        (tag.Element.Name != W.moveFromRangeStart &&
                        tag.Element.Name != W.moveFromRangeEnd))
                    {
                        foreach (var id in potentialDeletedElements)
                        {
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }

                        continue;
                    }
                }
            }
            var moveFromElementsToDelete = startElementTagsInMoveFromRange
                .Intersect(endElementTagsInMoveFromRange)
                .ToArray();
            if (moveFromElementsToDelete.Any())
            {
                return (XElement)AcceptMoveFromRangesTransform(
                    document, moveFromElementsToDelete);
            }

            return document;
        }

        private enum MoveFromCollectionType
        {
            ParagraphEndTagInMoveFromRange,
            Other
        };

        private static object AcceptParagraphEndTagsInMoveFromTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (W.BlockLevelContentContainers.Contains(element.Name))
                {
                    var groupedBodyChildren = element
                        .Elements()
                        .GroupAdjacent(c =>
                        {
                            var pi = c.GetParagraphInfo();
                            if (pi.ThisBlockContentElement != null)
                            {
                                var paragraphMarkIsInMoveFromRange =
                                    pi.ThisBlockContentElement.Elements(W.moveFromRangeStart).Any() &&
                                    !pi.ThisBlockContentElement.Elements(W.moveFromRangeEnd).Any();
                                if (paragraphMarkIsInMoveFromRange)
                                {
                                    return MoveFromCollectionType.ParagraphEndTagInMoveFromRange;
                                }
                            }
                            var previousContentElement = c.ContentElementsBeforeSelf()
                                .FirstOrDefault(e => e.GetParagraphInfo().ThisBlockContentElement != null);
                            if (previousContentElement != null)
                            {
                                var pi2 = previousContentElement.GetParagraphInfo();
                                if (c.Name == W.p &&
                                    pi2.ThisBlockContentElement.Elements(W.moveFromRangeStart).Any() &&
                                    !pi2.ThisBlockContentElement.Elements(W.moveFromRangeEnd).Any())
                                {
                                    return MoveFromCollectionType.ParagraphEndTagInMoveFromRange;
                                }
                            }
                            return MoveFromCollectionType.Other;
                        })
                        .ToList();

                    // If there is only one group, and it's key is MoveFromCollectionType.Other
                    // then there is nothing to do.
                    if (groupedBodyChildren.Count == 1 &&
                        groupedBodyChildren.First().Key == MoveFromCollectionType.Other)
                    {
                        var newElement = new XElement(element.Name,
                            element.Attributes(),
                            groupedBodyChildren.Select(g =>
                            {
                                if (g.Key == MoveFromCollectionType.Other)
                                {
                                    return g;
                                }

                                // This is a transform that produces the first element in the
                                // collection, except that the paragraph in the descendents is
                                // replaced with a new paragraph that contains all contents of the
                                // existing paragraph, plus subsequent elements in the group
                                // collection, where the paragraph in each of those groups is
                                // collapsed.
                                return CoalesqueParagraphEndTagsInMoveFromTransform(g.First(), g);
                            }));
                        return newElement;
                    }
                    else
                    {
                        return new XElement(element.Name,
                            element.Attributes(),
                            element.Nodes().Select(n =>
                                AcceptParagraphEndTagsInMoveFromTransform(n)));
                    }
                }
                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AcceptParagraphEndTagsInMoveFromTransform(n)));
            }
            return node;
        }

        private static object AcceptAllOtherRevisionsTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                // Accept inserted text, inserted paragraph marks, etc.
                // Collapse all w:ins elements.

                if (element.Name == W.ins)
                {
                    return element
                        .Nodes()
                        .Select(n => AcceptAllOtherRevisionsTransform(n));
                }

                // Remove all of the following elements.  These elements are processed in:
                //   AcceptDeletedAndMovedFromContentControls
                //   AcceptMoveFromMoveToTransform
                //   AcceptDeletedAndMoveFromParagraphMarksTransform
                //   AcceptParagraphEndTagsInMoveFromTransform
                //   AcceptMoveFromRanges

                if (element.Name == W.customXmlDelRangeStart ||
                    element.Name == W.customXmlDelRangeEnd ||
                    element.Name == W.customXmlInsRangeStart ||
                    element.Name == W.customXmlInsRangeEnd ||
                    element.Name == W.customXmlMoveFromRangeStart ||
                    element.Name == W.customXmlMoveFromRangeEnd ||
                    element.Name == W.customXmlMoveToRangeStart ||
                    element.Name == W.customXmlMoveToRangeEnd ||
                    element.Name == W.moveFromRangeStart ||
                    element.Name == W.moveFromRangeEnd ||
                    element.Name == W.moveToRangeStart ||
                    element.Name == W.moveToRangeEnd)
                {
                    return null;
                }

                // Accept revisions in formatting on paragraphs.
                // Accept revisions in formatting on runs.
                // Accept revisions for applied styles to a table.
                // Accept revisions for grid revisions to a table.
                // Accept revisions for column properties.
                // Accept revisions for row properties.
                // Accept revisions for table level property exceptions.
                // Accept revisions for section properties.
                // Accept numbering revision in fields.
                // Accept deleted field code text.
                // Accept deleted literal text.
                // Accept inserted cell.

                if (element.Name == W.pPrChange ||
                    element.Name == W.rPrChange ||
                    element.Name == W.tblPrChange ||
                    element.Name == W.tblGridChange ||
                    element.Name == W.tcPrChange ||
                    element.Name == W.trPrChange ||
                    element.Name == W.tblPrExChange ||
                    element.Name == W.sectPrChange ||
                    element.Name == W.numberingChange ||
                    element.Name == W.delInstrText ||
                    element.Name == W.delText ||
                    element.Name == W.cellIns)
                {
                    return null;
                }

                // Accept revisions for deleted math control character.
                // Match m:f/m:fPr/m:ctrlPr/w:del, remove m:f.

                if (element.Name == M.f &&
                    element.Elements(M.fPr).Elements(M.ctrlPr).Elements(W.del).Any())
                {
                    return null;
                }

                // Accept revisions for deleted rows in tables.
                // Match w:tr/w:trPr/w:del, remove w:tr.

                if (element.Name == W.tr &&
                    element.Elements(W.trPr).Elements(W.del).Any())
                {
                    return null;
                }

                // Accept deleted text in paragraphs.

                if (element.Name == W.del)
                {
                    return null;
                }

                // Accept revisions for vertically merged cells.
                //   cellMerge with a parent of tcPr, with attribute w:vMerge="rest" transformed
                //     to <w:vMerge w:val="restart"/>
                //   cellMerge with a parent of tcPr, with attribute w:vMerge="cont" transformed
                //     to <w:vMerge w:val="continue"/>

                if (element.Name == W.cellMerge &&
                    element.Parent.Name == W.tcPr &&
                    (string)element.Attribute(W.vMerge) == "rest")
                {
                    return new XElement(W.vMerge,
                        new XAttribute(W.val, "restart"));
                }

                if (element.Name == W.cellMerge &&
                    element.Parent.Name == W.tcPr &&
                    (string)element.Attribute(W.vMerge) == "cont")
                {
                    return new XElement(W.vMerge,
                        new XAttribute(W.val, "continue"));
                }

                // Otherwise do identity clone.
                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AcceptAllOtherRevisionsTransform(n)));
            }
            return node;
        }

        private static object CollapseParagraphTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.p)
                {
                    return element.Elements().Where(e => e.Name != W.pPr);
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => CollapseParagraphTransform(n)));
            }
            return node;
        }

        private enum DeletedParagraphCollectionType
        {
            DeletedParagraphMarkContent,
            ParagraphFollowing,
            Other
        };

        /// Accept deleted paragraphs.
        ///
        /// Group together all paragraphs that contain w:p/w:pPr/w:rPr/w:del elements.  Make a
        /// second group for the content element immediately following a paragraph that contains
        /// a w:del element.  The code uses the approach of dealing with paragraph content at
        /// 'levels', ignoring paragraph content at other levels.  Form a new paragraph that
        /// contains the content of the grouped paragraphs with deleted paragraph marks, and the
        /// content of the paragraph immediately following a paragraph that contains a deleted
        /// paragraph mark.  Include in the new paragraph the paragraph properties from the
        /// paragraph following.  When assembling the new paragraph, use a transform that collapses
        /// the paragraph nodes when adding content, thereby preserving custom XML and content
        /// controls.

        private static void AnnotateBlockContentElements(XElement contentContainer)
        {
            // For convenience, there is a ParagraphInfo annotation on the contentContainer.
            // It contains the same information as the ParagraphInfo annotation on the first
            //   paragraph.
            if (contentContainer.Annotation<BlockContentInfo>() != null)
            {
                return;
            }

            var firstContentElement = contentContainer
                .Elements()
                .DescendantsAndSelf()
                .FirstOrDefault(e => e.Name == W.p || e.Name == W.tbl);
            if (firstContentElement == null)
            {
                return;
            }

            // Add the annotation on the contentContainer.
            var currentContentInfo = new BlockContentInfo()
            {
                PreviousBlockContentElement = null,
                ThisBlockContentElement = firstContentElement,
                NextBlockContentElement = null
            };
            // Add as annotation even though NextParagraph is not set yet.
            contentContainer.AddAnnotation(currentContentInfo);
            while (true)
            {
                currentContentInfo.ThisBlockContentElement.AddAnnotation(currentContentInfo);
                // Find next sibling content element.
                XElement nextContentElement = null;
                var current = currentContentInfo.ThisBlockContentElement;
                while (true)
                {
                    nextContentElement = current
                        .ElementsAfterSelf()
                        .DescendantsAndSelf()
                        .FirstOrDefault(e => e.Name == W.p || e.Name == W.tbl);
                    if (nextContentElement != null)
                    {
                        currentContentInfo.NextBlockContentElement = nextContentElement;
                        break;
                    }
                    current = current.Parent;
                    // When we've backed up the tree to the contentContainer, we're done.
                    if (current == contentContainer)
                    {
                        return;
                    }
                }
                currentContentInfo = new BlockContentInfo()
                {
                    PreviousBlockContentElement = currentContentInfo.ThisBlockContentElement,
                    ThisBlockContentElement = nextContentElement,
                    NextBlockContentElement = null
                };
            }
        }

        private static IEnumerable<BlockContentInfo> IterateBlockContentElements(XElement element)
        {
            var current = element.Elements().FirstOrDefault();
            if (current == null)
            {
                yield break;
            }

            AnnotateBlockContentElements(element);
            var currentBlockContentInfo = element.Annotation<BlockContentInfo>();
            if (currentBlockContentInfo != null)
            {
                while (true)
                {
                    yield return currentBlockContentInfo;
                    if (currentBlockContentInfo.NextBlockContentElement == null)
                    {
                        yield break;
                    }

                    currentBlockContentInfo = currentBlockContentInfo.NextBlockContentElement.Annotation<BlockContentInfo>();
                }
            }
        }

        public static class PT
        {
            public static readonly XNamespace pt = "http://www.codeplex.com/PowerTools/2009/RevisionAccepter";
            public static readonly XName UniqueId = pt + "UniqueId";
            public static readonly XName RunIds = pt + "RunIds";
        }

        private static void AnnotateRunElementsWithId(XElement element)
        {
            var runId = 0;
            foreach (var e in element.Descendants().Where(e => e.Name == W.r))
            {
                if (e.Name == W.r)
                {
                    e.Add(new XAttribute(PT.UniqueId, runId++));
                }
            }
        }

        private static void AnnotateContentControlsWithRunIds(XElement element)
        {
            var sdtId = 0;
            foreach (var e in element.Descendants(W.sdt))
            {
                e.Add(new XAttribute(PT.RunIds,
                    e.DescendantsTrimmed(W.txbxContent)
                     .Where(d => d.Name == W.r)
                     .Select(r => r.Attribute(PT.UniqueId).Value)
                     .StringConcatenate(s => s + ",")
                     .Trim(',')),
                    new XAttribute(PT.UniqueId, sdtId++));
            }
        }

        private static XElement AddBlockLevelContentControls(XElement newDocument, XElement original)
        {
            var originalContentControls = original.Descendants(W.sdt).ToList();
            var existingContentControls = newDocument.Descendants(W.sdt).ToList();
            var contentControlsToAdd = originalContentControls
                .Select(occ => occ.Attribute(PT.UniqueId).Value)
                .Except(existingContentControls
                    .Select(ecc => ecc.Attribute(PT.UniqueId).Value));
            foreach (var contentControl in originalContentControls
                .Where(occ => contentControlsToAdd.Contains(occ.Attribute(PT.UniqueId).Value)))
            {
                // TODO - Need a slight modification here.  If there is a paragraph
                // in the content control that contains no runs, then the paragraph isn't included in the
                // content control, because the following triggers off of runs.
                // To see an example of this, see example document "NumberingParagraphPropertiesChange.docxs"

                // find list of runs to surround
                var runIds = contentControl.Attribute(PT.RunIds).Value.Split(',');
                var runs = contentControl.Descendants(W.r).Where(r => runIds.Contains(r.Attribute(PT.UniqueId).Value));
                // find the runs in the new document

                var runsInNewDocument = runs.Select(r => newDocument.Descendants(W.r).First(z => z.Attribute(PT.UniqueId).Value == r.Attribute(PT.UniqueId).Value)).ToList();

                // find common ancestor
                List<XElement> runAncestorIntersection = null;
                foreach (var run in runsInNewDocument)
                {
                    if (runAncestorIntersection == null)
                    {
                        runAncestorIntersection = run.Ancestors().ToList();
                    }
                    else
                    {
                        runAncestorIntersection = run.Ancestors().Intersect(runAncestorIntersection).ToList();
                    }
                }
                if (runAncestorIntersection == null)
                {
                    continue;
                }

                var commonAncestor = runAncestorIntersection.InDocumentOrder().Last();
                // find child of common ancestor that contains first run
                // find child of common ancestor that contains last run
                // create new common ancestor:
                //   elements before first run child
                //   add content control, and runs from first run child to last run child
                //   elements after last run child
                var firstRunChild = commonAncestor
                    .Elements()
                    .First(c => c.DescendantsAndSelf()
                        .Any(z => z.Name == W.r &&
                             z.Attribute(PT.UniqueId).Value == runsInNewDocument.First().Attribute(PT.UniqueId).Value));
                var lastRunChild = commonAncestor
                    .Elements()
                    .First(c => c.DescendantsAndSelf()
                        .Any(z => z.Name == W.r &&
                             z.Attribute(PT.UniqueId).Value == runsInNewDocument.Last().Attribute(PT.UniqueId).Value));

                // If the list of runs for the content control is exactly the list of runs for the paragraph, then
                // create the content control surrounding the paragraph, not surrounding the runs.

                if (commonAncestor.Name == W.p &&
                    commonAncestor.Elements()
                        .FirstOrDefault(e => e.Name != W.pPr && e.Name != W.commentRangeStart && e.Name != W.commentRangeEnd) == firstRunChild &&
                    commonAncestor.Elements()
                        .LastOrDefault(e => e.Name != W.pPr && e.Name != W.commentRangeStart && e.Name != W.commentRangeEnd) == lastRunChild)
                {
                    var newContentControlOrdered = new XElement(contentControl.Name,
                        contentControl.Attributes(),
                        contentControl.Elements().OrderBy(e =>
                        {
                            if (Order_sdt.ContainsKey(e.Name))
                            {
                                return Order_sdt[e.Name];
                            }

                            return 999;
                        }));

                    commonAncestor.ReplaceWith(newContentControlOrdered);
                    continue;
                }

                var elementsBeforeRange = commonAncestor
                    .Elements()
                    .TakeWhile(e => e != firstRunChild)
                    .ToList();
                var elementsInRange = commonAncestor
                    .Elements()
                    .SkipWhile(e => e != firstRunChild)
                    .TakeWhile(e => e != lastRunChild.ElementsAfterSelf().FirstOrDefault())
                    .ToList();
                var elementsAfterRange = commonAncestor
                    .Elements()
                    .SkipWhile(e => e != lastRunChild.ElementsAfterSelf().FirstOrDefault())
                    .ToList();

                // detatch from current parent
                commonAncestor.Elements().Remove();

                var newContentControl2 = new XElement(contentControl.Name,
                    contentControl.Attributes(),
                    contentControl.Elements().Where(e => e.Name != W.sdtContent),
                    new XElement(W.sdtContent, elementsInRange));

                var newContentControlOrdered2 = new XElement(newContentControl2.Name,
                    newContentControl2.Attributes(),
                    newContentControl2.Elements().OrderBy(e =>
                    {
                        if (Order_sdt.ContainsKey(e.Name))
                        {
                            return Order_sdt[e.Name];
                        }

                        return 999;
                    }));

                commonAncestor.Add(
                    elementsBeforeRange,
                    newContentControlOrdered2,
                    elementsAfterRange);
            }
            return newDocument;
        }

        private static readonly Dictionary<XName, int> Order_sdt = new Dictionary<XName, int>
        {
            { W.sdtPr, 10 },
            { W.sdtEndPr, 20 },
            { W.sdtContent, 30 },
            { W.bookmarkStart, 40 },
            { W.bookmarkEnd, 50 },
        };

        private static XElement AcceptDeletedAndMoveFromParagraphMarks(XElement element)
        {
            AnnotateRunElementsWithId(element);
            AnnotateContentControlsWithRunIds(element);
            var newElement = (XElement)AcceptDeletedAndMoveFromParagraphMarksTransform(element);
            var withBlockLevelContentControls = AddBlockLevelContentControls(newElement, element);
            return withBlockLevelContentControls;
        }

        private enum GroupingType
        {
            DeletedRange,
            Other,
        };

        private class GroupingInfo
        {
            public GroupingType GroupingType;
            public int GroupingKey;
        };

        private static object AcceptDeletedAndMoveFromParagraphMarksTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (W.BlockLevelContentContainers.Contains(element.Name))
                {
                    XElement bodySectPr = null;
                    if (element.Name == W.body)
                    {
                        bodySectPr = element.Element(W.sectPr);
                    }

                    var currentKey = 0;
                    var deletedParagraphGroupingInfo = new List<GroupingInfo>();

                    var state = 0; // 0 = in non deleted paragraphs
                                   // 1 = in deleted paragraph
                                   // 2 - paragraph following deleted paragraphs

                    foreach (var c in IterateBlockContentElements(element))
                    {
                        if (c.ThisBlockContentElement.Name == W.p)
                        {
                            var paragraphMarkIsDeletedOrMovedFrom = c
                                .ThisBlockContentElement
                                .Elements(W.pPr)
                                .Elements(W.rPr)
                                .Elements()
                                .Where(e => e.Name == W.del || e.Name == W.moveFrom)
                                .Any();

                            if (paragraphMarkIsDeletedOrMovedFrom)
                            {
                                if (state == 0 || state == 2)
                                {
                                    state = 1;
                                    currentKey += 1;
                                    deletedParagraphGroupingInfo.Add(
                                        new GroupingInfo()
                                        {
                                            GroupingType = GroupingType.DeletedRange,
                                            GroupingKey = currentKey,
                                        });
                                    continue;
                                }
                                else if (state == 1)
                                {
                                    deletedParagraphGroupingInfo.Add(
                                        new GroupingInfo()
                                        {
                                            GroupingType = GroupingType.DeletedRange,
                                            GroupingKey = currentKey,
                                        });
                                    continue;
                                }
                            }

                            if (state == 0)
                            {
                                currentKey += 1;
                                deletedParagraphGroupingInfo.Add(
                                    new GroupingInfo()
                                    {
                                        GroupingType = GroupingType.Other,
                                        GroupingKey = currentKey,
                                    });
                                continue;
                            }
                            else if (state == 1)
                            {
                                state = 2;
                                deletedParagraphGroupingInfo.Add(
                                    new GroupingInfo()
                                    {
                                        GroupingType = GroupingType.DeletedRange,
                                        GroupingKey = currentKey,
                                    });
                                continue;
                            }
                            else if (state == 2)
                            {
                                state = 0;
                                currentKey += 1;
                                deletedParagraphGroupingInfo.Add(
                                    new GroupingInfo()
                                    {
                                        GroupingType = GroupingType.Other,
                                        GroupingKey = currentKey,
                                    });
                                continue;
                            }
                        }
                        else if (c.ThisBlockContentElement.Name == W.tbl || c.ThisBlockContentElement.Name.Namespace == M.m)
                        {
                            currentKey += 1;
                            deletedParagraphGroupingInfo.Add(
                                new GroupingInfo()
                                {
                                    GroupingType = GroupingType.Other,
                                    GroupingKey = currentKey,
                                });
                            state = 0;
                            continue;
                        }
                        else
                        {
                            // otherwise keep the same state, put in the same group, and continue
                            deletedParagraphGroupingInfo.Add(
                                new GroupingInfo()
                                {
                                    GroupingType = GroupingType.Other,
                                    GroupingKey = currentKey,
                                });
                            continue;
                        }
                    }

                    var zipped = IterateBlockContentElements(element).Zip(deletedParagraphGroupingInfo, (blc, gi) => new
                    {
                        BlockLevelContent = blc,
                        GroupingInfo = gi,
                    });

                    var groupedParagraphs = zipped
                        .GroupAdjacent(z => z.GroupingInfo.GroupingKey);

                    // Create a new block level content container.
                    var newBlockLevelContentContainer = new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Where(e => e.Name == W.tcPr),
                        groupedParagraphs.Select((g, i) =>
                        {
                            if (g.First().GroupingInfo.GroupingType == GroupingType.DeletedRange)
                            {
                                var newParagraph = new XElement(W.p,
                                    g.Last().BlockLevelContent.ThisBlockContentElement.Elements(W.pPr),
                                    g.Select(z => CollapseParagraphTransform(z.BlockLevelContent.ThisBlockContentElement)));

                                // if this contains the last paragraph in the document, and if there is no content,
                                // and if the paragraph mark is deleted, then nuke the paragraph.
                                var allIsDeleted = AllParaContentIsDeleted(newParagraph);
                                if (allIsDeleted &&
                                    g.Last().BlockLevelContent.ThisBlockContentElement.Elements(W.pPr).Elements(W.rPr).Elements(W.del).Any() &&
                                    (g.Last().BlockLevelContent.NextBlockContentElement == null ||
                                     g.Last().BlockLevelContent.NextBlockContentElement.Name == W.tbl))
                                {
                                    return null;
                                }

                                return (object)newParagraph;
                            }
                            else
                            {
                                return g.Select(z =>
                                {
                                    var newEle = new XElement(z.BlockLevelContent.ThisBlockContentElement.Name,
                                        z.BlockLevelContent.ThisBlockContentElement.Attributes(),
                                        z.BlockLevelContent.ThisBlockContentElement.Nodes().Select(n => AcceptDeletedAndMoveFromParagraphMarksTransform(n)));
                                    return newEle;
                                });
                            }
                        }),
                        bodySectPr);

                    return newBlockLevelContentContainer;
                }

                // Otherwise, identity clone.
                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AcceptDeletedAndMoveFromParagraphMarksTransform(n)));
            }
            return node;
        }

        // Determine if the paragraph contains any content that is not deleted.
        private static bool AllParaContentIsDeleted(XElement p)
        {
            // needs collapse
            // dir, bdo, sdt, ins, moveTo, smartTag
            var testP = (XElement)CollapseTransform(p);

            var childElements = testP.Elements();
            var contentElements = childElements
                .Where(ce =>
                {
                    var b = IsRunContent(ce.Name);
                    if (b != null)
                    {
                        return (bool)b;
                    }

                    throw new Exception("Internal error 20, found element " + ce.Name.ToString());
                });
            if (contentElements.Any())
            {
                return false;
            }

            return true;
        }

        // dir, bdo, sdt, ins, moveTo, smartTag
        private static object CollapseTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.dir ||
                    element.Name == W.bdr ||
                    element.Name == W.ins ||
                    element.Name == W.moveTo ||
                    element.Name == W.smartTag)
                {
                    return element.Elements();
                }

                if (element.Name == W.sdt)
                {
                    return element.Elements(W.sdtContent).Elements();
                }

                if (element.Name == W.pPr)
                {
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => CollapseTransform(n)));
            }
            return node;
        }

        private static bool? IsRunContent(XName ceName)
        {
            // is content
            // r, fldSimple, hyperlink, oMath, oMathPara, subDoc
            if (ceName == W.r ||
                ceName == W.fldSimple ||
                ceName == W.hyperlink ||
                ceName == W.subDoc ||
                ceName == W.smartTag ||
                ceName == W.smartTagPr ||
                ceName.Namespace == M.m)
            {
                return true;
            }

            // not content
            // bookmarkStart, bookmarkEnd, commentRangeStart, commentRangeEnd, del, moveFrom, proofErr
            if (ceName == W.bookmarkStart ||
                ceName == W.bookmarkEnd ||
                ceName == W.commentRangeStart ||
                ceName == W.commentRangeEnd ||
                ceName == W.customXmlDelRangeStart ||
                ceName == W.customXmlDelRangeEnd ||
                ceName == W.customXmlInsRangeStart ||
                ceName == W.customXmlInsRangeEnd ||
                ceName == W.customXmlMoveFromRangeStart ||
                ceName == W.customXmlMoveFromRangeEnd ||
                ceName == W.customXmlMoveToRangeStart ||
                ceName == W.customXmlMoveToRangeEnd ||
                ceName == W.del ||
                ceName == W.moveFrom ||
                ceName == W.moveFromRangeStart ||
                ceName == W.moveFromRangeEnd ||
                ceName == W.moveToRangeStart ||
                ceName == W.moveToRangeEnd ||
                ceName == W.permStart ||
                ceName == W.permEnd ||
                ceName == W.proofErr)
            {
                return false;
            }

            return null;
        }

        private static IEnumerable<Tag> DescendantAndSelfTags(XElement element)
        {
            yield return new Tag
            {
                Element = element,
                TagType = TagTypeEnum.Element
            };
            var iteratorStack = new Stack<IEnumerator<XElement>>();
            iteratorStack.Push(element.Elements().GetEnumerator());
            while (iteratorStack.Count > 0)
            {
                if (iteratorStack.Peek().MoveNext())
                {
                    var currentXElement = iteratorStack.Peek().Current;
                    if (!currentXElement.Nodes().Any())
                    {
                        yield return new Tag()
                        {
                            Element = currentXElement,
                            TagType = TagTypeEnum.EmptyElement
                        };
                        continue;
                    }
                    yield return new Tag()
                    {
                        Element = currentXElement,
                        TagType = TagTypeEnum.Element
                    };
                    iteratorStack.Push(currentXElement.Elements().GetEnumerator());
                    continue;
                }
                iteratorStack.Pop();
                if (iteratorStack.Count > 0)
                {
                    yield return new Tag()
                    {
                        Element = iteratorStack.Peek().Current,
                        TagType = TagTypeEnum.EndElement
                    };
                }
            }
            yield return new Tag
            {
                Element = element,
                TagType = TagTypeEnum.EndElement
            };
        }

        private class PotentialInRangeElements
        {
            public List<XElement> PotentialStartElementTagsInRange;
            public List<XElement> PotentialEndElementTagsInRange;

            public PotentialInRangeElements()
            {
                PotentialStartElementTagsInRange = new List<XElement>();
                PotentialEndElementTagsInRange = new List<XElement>();
            }
        }

        private enum TagTypeEnum
        {
            Element,
            EndElement,
            EmptyElement
        }

        private class Tag
        {
            public XElement Element;
            public TagTypeEnum TagType;
        }

        private static object AcceptDeletedAndMovedFromContentControlsTransform(XNode node,
            XElement[] contentControlElementsToCollapse,
            XElement[] moveFromElementsToDelete)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.sdt && contentControlElementsToCollapse.Contains(element))
                {
                    return element
                        .Element(W.sdtContent)
                        .Nodes()
                        .Select(n => AcceptDeletedAndMovedFromContentControlsTransform(
                            n, contentControlElementsToCollapse, moveFromElementsToDelete));
                }

                if (moveFromElementsToDelete.Contains(element))
                {
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AcceptDeletedAndMovedFromContentControlsTransform(
                        n, contentControlElementsToCollapse, moveFromElementsToDelete)));
            }
            return node;
        }

        private static XElement AcceptDeletedAndMovedFromContentControls(XElement documentRootElement)
        {
            // The following lists contain the elements that are between start/end elements.
            var startElementTagsInDeleteRange = new List<XElement>();
            var endElementTagsInDeleteRange = new List<XElement>();
            var startElementTagsInMoveFromRange = new List<XElement>();
            var endElementTagsInMoveFromRange = new List<XElement>();

            // Following are the elements that *may* be in a range that has both start and end
            // elements.
            var potentialDeletedElements =
                new Dictionary<string, PotentialInRangeElements>();
            var potentialMoveFromElements =
                new Dictionary<string, PotentialInRangeElements>();

            foreach (var tag in DescendantAndSelfTags(documentRootElement))
            {
                if (tag.Element.Name == W.customXmlDelRangeStart)
                {
                    var id = tag.Element.Attribute(W.id).Value;
                    potentialDeletedElements.Add(id, new PotentialInRangeElements());
                    continue;
                }
                if (tag.Element.Name == W.customXmlDelRangeEnd)
                {
                    var id = tag.Element.Attribute(W.id).Value;
                    if (potentialDeletedElements.ContainsKey(id))
                    {
                        startElementTagsInDeleteRange.AddRange(
                            potentialDeletedElements[id].PotentialStartElementTagsInRange);
                        endElementTagsInDeleteRange.AddRange(
                            potentialDeletedElements[id].PotentialEndElementTagsInRange);
                        potentialDeletedElements.Remove(id);
                    }
                    continue;
                }
                if (tag.Element.Name == W.customXmlMoveFromRangeStart)
                {
                    var id = tag.Element.Attribute(W.id).Value;
                    potentialMoveFromElements.Add(id, new PotentialInRangeElements());
                    continue;
                }
                if (tag.Element.Name == W.customXmlMoveFromRangeEnd)
                {
                    var id = tag.Element.Attribute(W.id).Value;
                    if (potentialMoveFromElements.ContainsKey(id))
                    {
                        startElementTagsInMoveFromRange.AddRange(
                            potentialMoveFromElements[id].PotentialStartElementTagsInRange);
                        endElementTagsInMoveFromRange.AddRange(
                            potentialMoveFromElements[id].PotentialEndElementTagsInRange);
                        potentialMoveFromElements.Remove(id);
                    }
                    continue;
                }
                if (tag.Element.Name == W.sdt)
                {
                    if (tag.TagType == TagTypeEnum.Element)
                    {
                        foreach (var id in potentialDeletedElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                        }

                        foreach (var id in potentialMoveFromElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                        }

                        continue;
                    }
                    if (tag.TagType == TagTypeEnum.EmptyElement)
                    {
                        foreach (var id in potentialDeletedElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }
                        foreach (var id in potentialMoveFromElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }
                        continue;
                    }
                    if (tag.TagType == TagTypeEnum.EndElement)
                    {
                        foreach (var id in potentialDeletedElements)
                        {
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }

                        foreach (var id in potentialMoveFromElements)
                        {
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }

                        continue;
                    }
                    throw new PowerToolsInvalidDataException("Should not have reached this point.");
                }
                if (potentialMoveFromElements.Any() &&
                    tag.Element.Name != W.moveFromRangeStart &&
                    tag.Element.Name != W.moveFromRangeEnd &&
                    tag.Element.Name != W.customXmlMoveFromRangeStart &&
                    tag.Element.Name != W.customXmlMoveFromRangeEnd)
                {
                    if (tag.TagType == TagTypeEnum.Element)
                    {
                        foreach (var id in potentialMoveFromElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                        }

                        continue;
                    }
                    if (tag.TagType == TagTypeEnum.EmptyElement)
                    {
                        foreach (var id in potentialMoveFromElements)
                        {
                            id.Value.PotentialStartElementTagsInRange.Add(tag.Element);
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }
                        continue;
                    }
                    if (tag.TagType == TagTypeEnum.EndElement)
                    {
                        foreach (var id in potentialMoveFromElements)
                        {
                            id.Value.PotentialEndElementTagsInRange.Add(tag.Element);
                        }

                        continue;
                    }
                }
            }

            var contentControlElementsToCollapse = startElementTagsInDeleteRange
                .Intersect(endElementTagsInDeleteRange)
                .ToArray();
            var elementsToDeleteBecauseMovedFrom = startElementTagsInMoveFromRange
                .Intersect(endElementTagsInMoveFromRange)
                .ToArray();
            if (contentControlElementsToCollapse.Length > 0 ||
                elementsToDeleteBecauseMovedFrom.Length > 0)
            {
                var newDoc = AcceptDeletedAndMovedFromContentControlsTransform(documentRootElement,
                    contentControlElementsToCollapse, elementsToDeleteBecauseMovedFrom);
                return newDoc as XElement;
            }
            else
            {
                return documentRootElement;
            }
        }

        private static object AcceptMoveFromRangesTransform(XNode node,
            XElement[] elementsToDelete)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (elementsToDelete.Contains(element))
                {
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n =>
                        AcceptMoveFromRangesTransform(n, elementsToDelete)));
            }
            return node;
        }

        private static object CoalesqueParagraphEndTagsInMoveFromTransform(XNode node,
            IGrouping<MoveFromCollectionType, XElement> g)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.p)
                {
                    return new XElement(W.p,
                        element.Attributes(),
                        element.Elements(),
                        g.Skip(1).Select(p => CollapseParagraphTransform(p)));
                }
                else
                {
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Nodes().Select(n =>
                            CoalesqueParagraphEndTagsInMoveFromTransform(n, g)));
                }
            }
            return node;
        }

        private enum DeletedCellCollectionType
        {
            DeletedCell,
            Other
        };

        // For each table row, group deleted cells plus the cell before any deleted cell.
        // Produce a new cell that has gridSpan set appropriately for group, and clone everything
        // else.
        private static object AcceptDeletedCellsTransform(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.tr)
                {
                    var groupedCells = element
                        .Elements()
                        .GroupAdjacent(e =>
                        {
                            var cellAfter = e.ElementsAfterSelf(W.tc).FirstOrDefault();
                            var cellAfterIsDeleted = cellAfter != null &&
                                cellAfter.Descendants(W.cellDel).Any();
                            if (e.Name == W.tc &&
                                (cellAfterIsDeleted || e.Descendants(W.cellDel).Any()))
                            {
                                var a = new
                                {
                                    CollectionType = DeletedCellCollectionType.DeletedCell,
                                    Disambiguator = new[] { e }
                                        .Concat(e.SiblingsBeforeSelfReverseDocumentOrder())
                                        .FirstOrDefault(z => z.Name == W.tc &&
                                            !z.Descendants(W.cellDel).Any())
                                };
                                return a;
                            }
                            var a2 = new
                            {
                                CollectionType = DeletedCellCollectionType.Other,
                                Disambiguator = e
                            };
                            return a2;
                        });
                    var tr = new XElement(W.tr,
                        element.Attributes(),
                        groupedCells.Select(g =>
                        {
                            if (g.Key.CollectionType == DeletedCellCollectionType.DeletedCell
                                && g.First().Descendants(W.cellDel).Any())
                            {
                                return null;
                            }

                            if (g.Key.CollectionType == DeletedCellCollectionType.Other)
                            {
                                return g;
                            }

                            var gridSpanElement = g
                                .First()
                                .Elements(W.tcPr)
                                .Elements(W.gridSpan)
                                .FirstOrDefault();
                            var gridSpan = gridSpanElement != null ?
                                (int)gridSpanElement.Attribute(W.val) :
                                1;
                            var newGridSpan = gridSpan + g.Count() - 1;
                            var currentTcPr = g.First().Elements(W.tcPr).FirstOrDefault();
                            var newTcPr = new XElement(W.tcPr,
                                currentTcPr != null ? currentTcPr.Attributes() : null,
                                new XElement(W.gridSpan,
                                    new XAttribute(W.val, newGridSpan)),
                                currentTcPr.Elements().Where(e => e.Name != W.gridSpan));
                            var orderedTcPr = new XElement(W.tcPr,
                                newTcPr.Elements().OrderBy(e =>
                                {
                                    if (Order_tcPr.ContainsKey(e.Name))
                                    {
                                        return Order_tcPr[e.Name];
                                    }

                                    return 999;
                                }));
                            var newTc = new XElement(W.tc,
                                orderedTcPr,
                                g.First().Elements().Where(e => e.Name != W.tcPr));
                            return (object)newTc;
                        }));
                    return tr;
                }

                // Identity clone
                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => AcceptDeletedCellsTransform(n)));
            }
            return node;
        }

        private static readonly XName[] BlockLevelElements = new[] {
            W.p,
            W.tbl,
            W.sdt,
            W.del,
            W.ins,
            M.oMath,
            M.oMathPara,
            W.moveTo,
        };

        private static object RemoveRowsLeftEmptyByMoveFrom(XNode node)
        {
            var element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.tr)
                {
                    var nonEmptyCells = element.Elements(W.tc).Any(tc => tc.Elements().Any(tcc => BlockLevelElements.Contains(tcc.Name)));
                    if (nonEmptyCells)
                    {
                        return new XElement(element.Name,
                            element.Attributes(),
                            element.Nodes().Select(n => RemoveRowsLeftEmptyByMoveFrom(n)));
                    }
                    return null;
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => RemoveRowsLeftEmptyByMoveFrom(n)));
            }
            return node;
        }

        public static readonly XName[] TrackedRevisionsElements = new[]
        {
            W.cellDel,
            W.cellIns,
            W.cellMerge,
            W.customXmlDelRangeEnd,
            W.customXmlDelRangeStart,
            W.customXmlInsRangeEnd,
            W.customXmlInsRangeStart,
            W.del,
            W.delInstrText,
            W.delText,
            W.ins,
            W.moveFrom,
            W.moveFromRangeEnd,
            W.moveFromRangeStart,
            W.moveTo,
            W.moveToRangeEnd,
            W.moveToRangeStart,
            W.numberingChange,
            W.pPrChange,
            W.rPrChange,
            W.sectPrChange,
            W.tblGridChange,
            W.tblPrChange,
            W.tblPrExChange,
            W.tcPrChange,
            W.trPrChange,
        };

        public static bool PartHasTrackedRevisions(OpenXmlPart part)
        {
            return part.GetXDocument()
                .Descendants()
                .Any(e => TrackedRevisionsElements.Contains(e.Name));
        }

        public static bool HasTrackedRevisions(WmlDocument document)
        {
            using (var streamDoc = new OpenXmlMemoryStreamDocument(document))
            {
                using (var wdoc = streamDoc.GetWordprocessingDocument())
                {
                    return RevisionAccepter.HasTrackedRevisions(wdoc);
                }
            }
        }

        public static bool HasTrackedRevisions(WordprocessingDocument doc)
        {
            if (PartHasTrackedRevisions(doc.MainDocumentPart))
            {
                return true;
            }

            foreach (var part in doc.MainDocumentPart.HeaderParts)
            {
                if (PartHasTrackedRevisions(part))
                {
                    return true;
                }
            }

            foreach (var part in doc.MainDocumentPart.FooterParts)
            {
                if (PartHasTrackedRevisions(part))
                {
                    return true;
                }
            }

            if (doc.MainDocumentPart.EndnotesPart != null)
            {
                if (PartHasTrackedRevisions(doc.MainDocumentPart.EndnotesPart))
                {
                    return true;
                }
            }

            if (doc.MainDocumentPart.FootnotesPart != null)
            {
                if (PartHasTrackedRevisions(doc.MainDocumentPart.FootnotesPart))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public partial class WmlDocument : OpenXmlPowerToolsDocument
    {
        public WmlDocument AcceptRevisions(WmlDocument document)
        {
            return RevisionAccepter.AcceptRevisions(document);
        }

        public bool HasTrackedRevisions(WmlDocument document)
        {
            return RevisionAccepter.HasTrackedRevisions(document);
        }
    }

    public class BlockContentInfo
    {
        public XElement PreviousBlockContentElement { get; set; }
        public XElement ThisBlockContentElement { get; set; }
        public XElement NextBlockContentElement { get; set; }
    }

    public static class RevisionAccepterExtensions
    {
        private static void InitializeParagraphInfo(XElement contentContext)
        {
            if (!(W.BlockLevelContentContainers.Contains(contentContext.Name)))
            {
                throw new ArgumentException(
                    "GetParagraphInfo called for element that is not child of content container");
            }

            XElement prev = null;
            foreach (var content in contentContext.Elements())
            {
                // This may return null, indicating that there is no descendant paragraph.  For
                // example, comment elements have no descendant elements.
                var paragraph = content
                    .DescendantsAndSelf()
                    .FirstOrDefault(e => e.Name == W.p || e.Name == W.tc || e.Name == W.txbxContent);
                if (paragraph != null &&
                    (paragraph.Name == W.tc || paragraph.Name == W.txbxContent))
                {
                    paragraph = null;
                }

                var pi = new BlockContentInfo()
                {
                    PreviousBlockContentElement = prev,
                    ThisBlockContentElement = paragraph
                };
                content.AddAnnotation(pi);
                prev = content;
            }
        }

        public static BlockContentInfo GetParagraphInfo(this XElement contentElement)
        {
            var paragraphInfo = contentElement.Annotation<BlockContentInfo>();
            if (paragraphInfo != null)
            {
                return paragraphInfo;
            }

            InitializeParagraphInfo(contentElement.Parent);
            return contentElement.Annotation<BlockContentInfo>();
        }

        public static IEnumerable<XElement> ContentElementsBeforeSelf(this XElement element)
        {
            var current = element;
            while (true)
            {
                var pi = current.GetParagraphInfo();
                if (pi.PreviousBlockContentElement == null)
                {
                    yield break;
                }

                yield return pi.PreviousBlockContentElement;
                current = pi.PreviousBlockContentElement;
            }
        }
    }
}

// Markup that this code processes:
//
// delText
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: MovedText.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to w:t element
//
// del (deleted run content)
//   Method: AcceptAllOtherRevisionsTransform
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements and descendant elements.
//   Reject:
//     Transform to w:ins element
//     Then Accept
//
// ins (inserted run content)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: InsertedParagraphsAndRuns.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Collapse these elements.
//   Reject:
//     Transform to w:del element, and child w:t transform to w:delText element
//     Then Accept
//
// ins (inserted paragraph)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: InsertedParagraphsAndRuns.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to w:del element
//     Then Accept
//
// del (deleted paragraph mark)
//   Method: AcceptDeletedAndMoveFromParagraphMarksTransform
//   Sample document: VariousTableRevisions.docx (deleted paragraph mark in paragraph in
//     content control)
//   Reviewed: tristan and zeyad ****************************************
//   Semantics:
//     Find all adjacent paragraps that have this element.
//     Group adjacent paragraphs plus the paragraph following paragraph that has this element.
//     Replace grouped paragraphs with a new paragraph containing the content from all grouped
//       paragraphs.  Use the paragraph properties from the first paragraph in the group.
//   Reject:
//     Transform to w:ins element
//     Then Accept
//
// del (deleted table row)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Match w:tr/w:trPr/w:del, remove w:tr.
//   Reject:
//     Transform to w:ins
//     Then Accept
//
// ins (inserted table row)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to w:del
//     Then Accept
//
// del (deleted math control character)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: DeletedMathControlCharacter.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Match m:f/m:fPr/m:ctrlPr/w:del, remove m:f.
//   Reject:
//     Transform to w:ins
//     Then Accept
//
// ins (inserted math control character)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: InsertedMathControlCharacter.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to w:del
//     Then Accept
//
// moveTo (move destination paragraph mark)
//   Method: AcceptMoveFromMoveToTransform
//   Sample document: MovedText.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to moveFrom
//     Then Accept
//
// moveTo (move destination run content)
//   Method: AcceptMoveFromMoveToTransform
//   Sample document: MovedText.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Collapse these elements.
//   Reject:
//     Transform to moveFrom
//     Then Accept
//
// moveFrom (move source paragraph mark)
//   Methods: AcceptDeletedAndMoveFromParagraphMarksTransform, AcceptParagraphEndTagsInMoveFromTransform
//   Sample document: MovedText.docx
//   Reviewed: tristan and zeyad ****************************************
//   Semantics:
//     Find all adjacent paragraps that have this element or deleted paragraph mark.
//     Group adjacent paragraphs plus the paragraph following paragraph that has this element.
//     Replace grouped paragraphs with a new paragraph containing the content from all grouped
//       paragraphs.
//     This is handled in the same code that handles del (deleted paragraph mark).
//   Reject:
//     Transform to moveTo
//     Then Accept
//
// moveFrom (move source run content)
//   Method: AcceptMoveFromMoveToTransform
//   Sample document: MovedText.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to moveTo
//     Then Accept
//
// moveFromRangeStart
// moveFromRangeEnd
//   Method: AcceptMoveFromRanges
//   Sample document: MovedText.docx
//   Semantics:
//     Find pairs of elements.  Remove all elements that have both start and end tags in a
//       range.
//   Reject:
//     Transform to moveToRangeStart, moveToRangeEnd
//     Then Accept
//
// moveToRangeStart
// moveToRangeEnd
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: MovedText.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to moveFromRangeStart, moveFromRangeEnd
//     Then Accept
//
// customXmlDelRangeStart
// customXmlDelRangeEnd
// customXmlMoveFromRangeStart
// customXmlMoveFromRangeEnd
//   Method: AcceptDeletedAndMovedFromContentControls
//   Reviewed: tristan and zeyad ****************************************
//   Semantics:
//     Find pairs of start/end elements, matching id attributes.  Collapse sdt
//       elements that have both start and end tags in a range.
//   Reject:
//     Transform to customXmlInsRangeStart, customXmlInsRangeEnd, customXmlMoveToRangeStart, customXmlMoveToRangeEnd
//     Then Accept
//
// customXmlInsRangeStart
// customXmlInsRangeEnd
// customXmlMoveToRangeStart
// customXmlMoveToRangeEnd
//   Method: AcceptAllOtherRevisionsTransform
//   Reviewed: tristan and zeyad ****************************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to customXmlDelRangeStart, customXmlDelRangeEnd, customXmlMoveFromRangeStart, customXmlMoveFromRangeEnd
//     Then Accept
//
// delInstrText (deleted field code)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: NumberingParagraphPropertiesChange.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Transform to instrText
//     Then Accept
//     Note that instrText must be transformed to delInstrText when in a w:ins, in the same fashion that w:t must be transformed to w:delText when in w:ins
//
// ins (inserted numbering properties)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: InsertedNumberingProperties.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject
//     Remove the containing w:numPr
//
// pPrChange (revision information for paragraph properties)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: ParagraphAndRunPropertyRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace pPr with the pPr in pPrChange
//
// rPrChange (revision information for run properties)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: ParagraphAndRunPropertyRevisions.docx
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace rPr with the rPr in rPrChange
//
// rPrChange (revision information for run properties on the paragraph mark)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: ParagraphAndRunPropertyRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace rPr with the rPr in rPrChange.
//
// numberingChange (previous numbering field properties)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: NumberingFieldPropertiesChange.docx
//   Semantics:
//     Remove these elements.
//   Reject:
//     Remove these elements.
//     These are there for numbering created via fields, and are not important.
//
// numberingChange (previous paragraph numbering properties)
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: NumberingFieldPropertiesChange.docx
//   Semantics:
//     Remove these elements.
//   Reject:
//     Remove these elements.
//
// sectPrChange
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: SectionPropertiesChange.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace sectPr with the sectPr in sectPrChange
//
// tblGridChange
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: TableGridChange.docx
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace tblGrid with the tblGrid in tblGridChange
//
// tblPrChange
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: TableGridChange.docx
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace tblPr with the tblPr in tblPrChange
//
// tblPrExChange
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace tblPrEx with the tblPrEx in tblPrExChange
//
// tcPrChange
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: TableGridChange.docx
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace tcPr with the tcPr in tcPrChange
//
// trPrChange
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: VariousTableRevisions.docx
//   Reviewed: zeyad ***************************
//   Semantics:
//     Remove these elements.
//   Reject:
//     Replace trPr with the trPr in trPrChange
//
// celDel
//   Method: AcceptDeletedCellsTransform
//   Sample document: HorizontallyMergedCells.docx
//   Semantics:
//     Group consecutive deleted cells, and remove them.
//     Adjust the cell before deleted cells:
//       Increase gridSpan by the number of deleted cells that are removed.
//   Reject:
//     Remove this element
//
// celIns
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: HorizontallyMergedCells11.docx
//   Semantics:
//     Remove these elements.
//   Reject:
//     If a w:tc contains w:tcPr/w:cellIns, then remove the cell
//
// cellMerge
//   Method: AcceptAllOtherRevisionsTransform
//   Sample document: MergedCell.docx
//   Semantics:
//     Transform cellMerge with a parent of tcPr, with attribute w:vMerge="rest"
//       to <w:vMerge w:val="restart"/>.
//     Transform cellMerge with a parent of tcPr, with attribute w:vMerge="cont"
//       to <w:vMerge w:val="continue"/>
//
// The following items need to be addressed in a future release:
// - inserted run inside deleted paragraph - moveTo is same as insert
// - must increase w:val attribute of the w:gridSpan element of the
//   cell immediately preceding the group of deleted cells by the
//   ***sum*** of the values of the w:val attributes of w:gridSpan
//   elements of each of the deleted cells.