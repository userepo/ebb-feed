namespace IC.EbbFeedWorkerService.Models
{
    public class Notice
    {
        /// <summary>
        /// Transportation Service Provider (TSP) name
        /// </summary>
        public string? TspName { get; set; }

        /// <summary>
        /// Notice Type, e.g., "Critical", "Planned Outage", "Maintenance" 
        /// </summary>
        public string? NoticeType { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public DateTime PostedDateTime { get; set; }
        public DateTime EffectiveDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public string? SegmentLocation { get; set; }

        /// <summary>
        /// Link to the full notice
        ///</summary>
        public string? NoticeUrl { get; set; }

        public string? CurtailmentVolumes { get; set; }
    }
}
