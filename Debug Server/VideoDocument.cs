namespace Debug_Server
{
    internal class VideoDocument
    {
        public VideoDocument(byte[] video)
        {
            _id = System.Guid.NewGuid().ToString();
            Video = video;
        }
        public string _id { get; set; }
        public byte[] Video { get; set; }
    }
}