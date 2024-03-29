﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MongoDB.Driver;
using PuppeteerSharp;

namespace ConsoleCrawler
{
    class Program
    {
        private static string url = "https://metruyenchu.com";
        private static string urlWithPage = "https://metruyenchu.com/truyen?sort_by=created_at&status=-1&props=-1&limit=30&page=";
        private static IMongoCollection<Commic> _commic;
        static async Task Main(string[] args)
        {
            var client = new MongoClient("mongodb+srv://enter0208:8NzaSkZdvPE6MU9@cluster0.cixty.mongodb.net/myFirstDatabase?retryWrites=true&w=majority");
            var database = client.GetDatabase("CrawlerDB");
            _commic = database.GetCollection<Commic>("Commics");

            double von = 23500;
            for (int i = 1; i <= 75; i++)
            {
                von = von + (von * 0.015);
            }
            var a = von;

            Console.WriteLine("Choose Crawler : ");
            Console.WriteLine("1. Commic");
            Console.WriteLine("2. Chapter");
            var choose = Int16.Parse(Console.ReadLine());

            if (choose == 1)
            {
                Console.WriteLine("You choose crawler commics");
                var browserFetcher = new BrowserFetcher();
                var process = await browserFetcher.DownloadAsync();
                if (process.Downloaded)
                {
                    Console.WriteLine("Install chronium success");
                }
                else
                {
                    Console.WriteLine("Install chronium fail");
                }
                Console.WriteLine("Many thread ?");
                var threads = Int16.Parse(Console.ReadLine());
                Console.WriteLine("Crawling data commic ...., please wait for it!");
                await CrawlerCommics(threads);
            }
            else
            {
                Console.WriteLine("you choose crawler commics");
                var browserFetcher = new BrowserFetcher();
                var process = await browserFetcher.DownloadAsync();
                if (process.Downloaded)
                {
                    Console.WriteLine("Install chronium success");
                }
                else
                {
                    Console.WriteLine("Install chronium fail");
                }
                Console.WriteLine("Crawling data chapter ...., please wait for it!");
            }
        }

        private static async Task CrawlerCommics(int thread)
        {
            string html = await loadPage(url + "/truyen");
            var pagesSite = Int16.Parse(await getLengthPages(html));
            var loop = (pagesSite) / thread;
            for (int i = 0; i < loop; i++)
            {
                var series = Enumerable.Range((i * thread) + 1, thread).ToList();
                await Task.WhenAll(series.Select(s => DoWorkAsync(s)));
            }
            if ((pagesSite % thread) > 0)
            {
                var series = Enumerable.Range((loop * thread) + 1, pagesSite % thread).ToList();
                await Task.WhenAll(series.Select(s => DoWorkAsync(s)));
            }
        }
        private static async Task DoWorkAsync(int i)
        {
            Console.WriteLine($"Starting Process {i}: " + DateTime.Now.ToString("yyyy-dd-MM-HH:mm:ss"));
            var htmlContent = await loadPage(urlWithPage + i.ToString());
            if (string.IsNullOrEmpty(htmlContent))
            {
                Console.WriteLine("Fail loaded" + i);
            }
            else
            {
                var document = new HtmlDocument();
                document.LoadHtml(htmlContent);
                var documentDoc = document.DocumentNode;
                var node = documentDoc.SelectNodes(".//div[@class='media border-bottom py-4']");
                var lstCommicsPage = new List<Commic>();
                foreach (var item in node)
                {
                    Commic commic = new Commic();
                    commic.Link = item.FirstChild.Attributes["href"].Value;
                    commic.Img = item.FirstChild.ChildNodes[0].Attributes["data-src"].Value;
                    commic.Name = item.ChildNodes[2].FirstChild.InnerText.Trim();
                    commic.Description = item.ChildNodes[2].ChildNodes[2].InnerText.Trim();
                    commic.Author = item.ChildNodes[2].ChildNodes[4].FirstChild.ChildNodes[0].InnerText.Trim();
                    string urlCommic = url + commic.Link;
                    var htmlCommic = await loadPage(urlCommic);
                    var documentCommic = new HtmlDocument();
                    documentCommic.LoadHtml(htmlCommic);
                    commic.Status = documentCommic.DocumentNode.SelectSingleNode(".//li[@class='d-inline-block border border-danger px-3 py-1 text-danger rounded-3 mr-2 mb-2']").InnerText;
                    commic.Category = documentCommic.DocumentNode.SelectSingleNode(".//li[@class='d-inline-block border border-primary px-3 py-1 text-primary rounded-3 mr-2 mb-2']").InnerText;
                    var listMotips = documentCommic.DocumentNode.SelectNodes(".//li[@class='d-inline-block border border-success px-3 py-1 text-success rounded-3 mr-2 mb-2']");
                    var valueMotipcs = new List<string>();
                    if (listMotips != null)
                    {
                        for (int j = 0; j < listMotips.Count; j++)
                        {
                            valueMotipcs.Add(listMotips[j].InnerText);
                        }
                    }
                    commic.Motips = valueMotipcs;
                    var info = documentCommic.DocumentNode.SelectNodes(".//li[@class='mr-5']");
                    commic.LengthChapter = info[0].FirstChild.InnerText;
                    commic.Performance = info[1].FirstChild.InnerText;
                    commic.Reads = info[2].FirstChild.InnerText;
                    commic.Rating = documentCommic.DocumentNode.SelectSingleNode(".//span[@class='d-inline-block ml-2']").InnerText;
                    lstCommicsPage.Add(commic);
                }
                await _commic.InsertManyAsync(lstCommicsPage);
                Console.WriteLine($"Ending Process {i}: " + DateTime.Now.ToString("yyyy-dd-MM-HH:mm:ss") + "Total insert : " + lstCommicsPage.Count());
            }
        }

        private static async Task CrawlerChapter(int thread, int chapters)
        {
            var lstCommics = await _commic.Find(c => true).ToListAsync();
            var pagations = lstCommics.Count() / chapters;
            for (int i = 0; i < pagations; i++)
            {
                var listPagations = lstCommics.Skip(i * chapters).Take(chapters).ToList();
                var loop = lstCommics.Count() / thread;
                for (int l = 0; l < loop; l++)
                {
                    var series = Enumerable.Range((i * thread) + 1, thread).ToList();
                    await Task.WhenAll(series.Select(s => WorkerChapters(listPagations[s].Link, listPagations[s].LengthChapter)));
                }
            }
        }

        private static async Task WorkerChapters(string link, string chapters)
        {

        }


        private static async Task<string> loadPage(string urlPage)
        {
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new string[] { "--no-sandbox", "--disable-setuid-sandbox" },
                IgnoredDefaultArgs = new string[] { "--disable-extensions" }
            });
            await using var page = await browser.NewPageAsync();

            await page.SetUserAgentAsync("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
            await page.GoToAsync(urlPage, timeout: 0);
            await Task.Delay(10000);
            string result = await page.GetContentAsync();
            await page.CloseAsync();
            await browser.CloseAsync();
            return result;
        }

        private static Task<string> getLengthPages(string htmlContent)
        {
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);
            return Task.FromResult(document.DocumentNode.SelectNodes(".//ul[@class='pagination pagination-sm']/li")[6].InnerText);
        }
    }
}
