﻿using System;
using System.Collections.Generic;
using System.Linq;
using Hawk.Standard.Interfaces;
using Hawk.Standard.Plugins.Transformers;
using Hawk.Standard.Utils;
using Hawk.Standard.Utils.Plugins;
using HtmlAgilityPack;
using GlobalHelper = Hawk.Standard.Interfaces.GlobalHelper;

namespace Hawk.Standard.Crawlers
{
    [XFrmWork("XPathTF", "XPathTF_desc")]
    public class XPathTF : TransformerBase
    {
        public XPathTF()
        {
            IsManyData = ScriptWorkMode.One;
            SelectorFormat= SelectorFormat.XPath;
            XPath = "";
        }
        [BrowsableAttribute.PropertyOrderAttribute(3)]
        [LocalizedDisplayName("key_163")]
        public string XPath { get; set; }

        [BrowsableAttribute.PropertyOrderAttribute(2)]
        [LocalizedDisplayName("key_188")]
        [LocalizedDescription("etl_script_mode")]
        public ScriptWorkMode IsManyData { get; set; }

        [BrowsableAttribute.LocalizedCategoryAttribute("key_190")]
        [LocalizedDisplayName("key_191")]
        [LocalizedDescription("key_192")]
        public bool GetText { get; set; }


        [BrowsableAttribute.PropertyOrderAttribute(0)]
        [LocalizedDisplayName("key_162")]
        [LocalizedDescription("")]

        public SelectorFormat SelectorFormat { get; set; }

        [BrowsableAttribute.PropertyOrderAttribute(1)]
        [LocalizedDisplayName("key_193")]
        [LocalizedDescription("")]
        public CrawlType CrawlType { get; set; }


        [Browsable(false)]
        public override string KeyConfig => XPath;


        private List<string> htmls=new List<string>(); 
        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument data)
        {
            var item = data[Column];
            var docu = new HtmlDocument();
            if(htmls.Count<5)
                htmls.Add(item.ToString());
            

            docu.LoadHtml(item.ToString());
           var  path = data.Query(XPath);

            var p2 = docu.DocumentNode.SelectNodes(path, this.SelectorFormat);
            if (p2 == null)
                return new List<IFreeDocument>();
            return p2.Select(node =>
            {
                var doc = new FreeDocument();
               
                 doc.MergeQuery(data, NewColumn);
                doc.SetValue("Text", node.GetNodeText());
                doc.SetValue("HTML", node.InnerHtml);
                doc.SetValue("OHTML", node.OuterHtml);
                return doc;
            });
        }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = IsManyData==ScriptWorkMode.List;
            htmls = new List<string>();
            return base.Init(docus);
        }

        public override object TransformData(IFreeDocument document)
        {
            var item = document[Column];
            if (htmls.Count < 5)
                htmls.Add(item.ToString());
            if (item is IFreeDocument)
            {
                return (item as IFreeDocument).GetDataFromXPath(XPath);
            }
            var docu = new HtmlDocument();

            docu.LoadHtml(item.ToString());
            string path;
            if (GetText)
            {
                 path = docu.DocumentNode.GetTextNode();
                return docu.DocumentNode.GetDataFromXPath(path, CrawlType);
            }
            else
            {
                 path = document.Query(XPath);
                return docu.DocumentNode.GetDataFromXPath(path, CrawlType, SelectorFormat);


            }
          
        }
    }
  
    [XFrmWork("XPathTF2", "xpath2_desc")]
    public class XPathTF2 : ResponseTF
    {
        private Dictionary<string, string> xpaths;

        [Browsable(false)]
        public override string HeaderFilter { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            base.Init(docus);
            if (Crawler == null)
                return false;
            IsMultiYield = true;
            xpaths = Crawler.CrawlItems.GroupBy(d => d.Name).Select(d =>
            {
                var column = d.Key;
                var path = XPath.GetMaxCompareXPath(d.Select(d2 => d2.XPath).ToList());
                return new {Column = column, XPath = path};
            }).ToDictionary(d => d.Column, d => d.XPath);
            return true;
        }

        private IEnumerable<IFreeDocument> Get(HtmlDocument docu, IEnumerable<IFreeDocument> source, string name,
            string xpath)
        {
            HtmlNodeCollection nodes;
            try
            {
                nodes = docu.DocumentNode.SelectNodes(xpath);
            }
            catch (Exception ex)
            {
                XLogSys.Print.Warn(GlobalHelper.Get("key_196") + xpath);
                return source;
            }
            if (nodes.Count == 0)
            {
                XLogSys.Print.Warn(GlobalHelper.Get("key_197") + xpath + GlobalHelper.Get("key_198"));
                return source;
            }
            var new_docs = nodes.Select(node =>
            {
                var doc = new FreeDocument();
                doc.Add(name + "_text", node.GetNodeText());
                doc.Add(name + "_ohtml", node.OuterHtml);
                return doc;
            });
            return new_docs.Cross(source);
        }

        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument data)
        {
            {
                var item = data[Column];
                var docu = new HtmlDocument();

                docu.LoadHtml(item.ToString());
                var d = new FreeDocument();
                d.MergeQuery(data, NewColumn);
                IEnumerable<IFreeDocument> source = new List<IFreeDocument> {d};
                source = xpaths.Aggregate(source, (current, xpath) => Get(docu, current, xpath.Key, xpath.Value));
                return source.ToList();
            }
        }
    }
}