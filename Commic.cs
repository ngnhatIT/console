using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConsoleCrawler
{
    public class Commic
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Img { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string LengthChapter { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public string Rating { get; set; }
        public string Performance { get; set; }
        public string Reads { get; set; }
        public List<string> Motips { get; set; }
    }
}