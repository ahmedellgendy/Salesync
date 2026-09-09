using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Salesync.Application.Common.Licensing;

namespace Salesync.Infrastructure.Licensing
{
    public class LicenseSignatureVerifier
        : ILicenseSignatureVerifier
    {
        private const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAz4I+HzlfCq/OFuwD43Ku
        5IghzbcX/BMQcaSnbZWILAiWAyuKeZU2Lzf3NcjT/pyq1ziY9JCFsX6E0fDjV3Sh
        XgQYJttx1z2G6WhUfEnm0c0BZz7fMMkQmuUapkWirOsu8Ug7BN63bcadTd2fW2+/
        KqERWGjkJCKoLpnqUi/ap4UKLhjWI5CRiC9CCzzqouTVi+Uc5t+HK3jHRIfycS1x
        y7fy0c0611FpRrPXcBvLXkTNQ1HQo+XKdytVl8QbMjhnJ4/4JvwkCOY+oOgLVskO
        MN7jkOpKgUMQGINS7j4nHNU4Dj369GVPjG0JO7vkFzhIywf/6jxNlgj+PhP+occi
        AQIDAQAB
        -----END PUBLIC KEY-----
        """;


        public bool Verify(LicensePayload payload, string signature)
        {
            if (payload is null)
                return false;

            if (string.IsNullOrWhiteSpace(signature))
                return false;


            try
            {
                var payloadJson =
                    JsonSerializer.Serialize(
                        payload,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy =
                                JsonNamingPolicy.CamelCase,

                            WriteIndented =
                                false
                        });


                var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

                var signatureBytes = Convert.FromBase64String(signature);

                using var rsa = RSA.Create();

                rsa.ImportFromPem(PublicKeyPem);

                return rsa.VerifyData(
                    payloadBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }
    }
}