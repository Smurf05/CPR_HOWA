namespace HOWA.Domain.DTOs
{
    public class OtpVerifyResult
    {
        public bool   Success    { get; set; }
        public int    LogId      { get; set; }
        public string Status     { get; set; } = string.Empty; // 'Present' | 'Late'
        public string Message    { get; set; } = string.Empty;
    }
}
