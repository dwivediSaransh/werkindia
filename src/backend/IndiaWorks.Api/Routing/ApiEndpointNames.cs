namespace IndiaWorks.Api.Routing;

public static class ApiEndpointNames
{
    public const string Health = "IndiaWorks.Health";
    public const string Readiness = "IndiaWorks.Readiness";
    public const string RequestOtp = "IndiaWorks.Auth.Otp.Request";
    public const string VerifyOtp = "IndiaWorks.Auth.Otp.Verify";
    public const string RefreshToken = "IndiaWorks.Auth.Otp.Refresh";
    public const string Logout = "IndiaWorks.Auth.Otp.Logout";
    public const string ReceivePaymentWebhook = "IndiaWorks.Billing.Webhooks.ReceivePayment";
}
