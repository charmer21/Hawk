﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hawk.Standard.Plugins.Generators;
using Hawk.Standard.Utils.Plugins;

namespace Hawk.Standard.Plugins.Transformers
{
    [XFrmWork("CrawlerTF", "CrawlerTF_desc", "carema")]
    public class CrawlerTF : ResponseTF
    {
        private BfsGE generator;

        public CrawlerTF()
        {
            PropertyChanged += (s, e) => { buffHelper.Clear(); };
        }
        static  SmartCrawler defaultCrawler=new SmartCrawler();

        [Browsable(false)]
        public override string HeaderFilter { get; set; }

        [PropertyOrder(1)]
        [LocalizedDisplayName("key_482")]
        public string PostData { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);

            IsMultiYield = Crawler?.IsMultiData == ScriptWorkMode.List && Crawler.CrawlItems.Count > 0;
            return true;
        }
        [PropertyOrder(2)]
        [LocalizedDisplayName("key_118")]
        public string Proxy { get; set; }
        [Browsable(false)]
        public override string KeyConfig => CrawlerSelector.SelectItem;
        private IEnumerable<FreeDocument> GetDatas(IFreeDocument data)
        {
            var p = data[Column];
            if (p == null || Crawler == null)
                return new List<FreeDocument>();
            var url = p.ToString();
            var bufkey = url;
            var post = data.Query(PostData);
            var crawler = Crawler;
            if (crawler == null)
            {
                crawler = defaultCrawler;
            }
            if (crawler.Http.Method == MethodType.POST)
            {
                bufkey += post;
            }
            var htmldoc = buffHelper.Get(bufkey);

            if (htmldoc == null)
            {
                HttpStatusCode code;

                var count = 0;
                var docs = crawler.CrawlData(url, out htmldoc, out code, post);
                if (HttpHelper.IsSuccess(code))
                {
                    buffHelper.Set(bufkey, htmldoc);
                    return docs;
                }
                throw new Exception("Web Request Error:" + code);
            }
            return crawler.CrawlData(htmldoc.DocumentNode);
        }

        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument data)
        {
            var docs = GetDatas(data);
            return docs.Select<FreeDocument, IFreeDocument>(d => d.MergeQuery(data, NewColumn));
        }

        public override object TransformData(IFreeDocument datas)
        {
            var docs = GetDatas(datas);
            var first = docs.FirstOrDefault();
            if (first != null)
            {
                first.DictCopyTo(datas);
            }

            return null;
        }
    }
}