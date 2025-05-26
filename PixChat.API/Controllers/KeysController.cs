using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.Interfaces.Services;

namespace PixChat.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KeysController : ControllerBase
    {
        private readonly IKeyService _keyService;

        public KeysController(IKeyService keyService)
        {
            _keyService = keyService;
        }

        [HttpPost("{userId}/generate")]
        public async Task<IActionResult> GenerateKeys(int userId)
        {
            try
            {
                var (publicKey, privateKey) = await _keyService.GenerateKeyPairAsync();

                await _keyService.SaveKeysAsync(userId, publicKey, privateKey);

                return Ok(new { PublicKey = publicKey, PrivateKey = privateKey });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error generating keys", error = ex.Message });
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetKeys(int userId)
        {
            try
            {
                var publicKey = await _keyService.GetPublicKeyAsync(userId);
                var privateKey = await _keyService.GetPrivateKeyAsync(userId);

                return Ok(new { PublicKey = publicKey, PrivateKey = privateKey });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error retrieving keys", error = ex.Message });
            }
        }

        [HttpPost("decrypt-aes-key")]
        public async Task<IActionResult> DecryptAesKey([FromBody] DecryptAesKeyRequest request)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(request.PrivateKey);

                var encryptedAesKey = Convert.FromBase64String(request.EncryptedAESKey);
                var decryptedAesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

                return Ok(Convert.ToBase64String(decryptedAesKey));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error decrypting AES key", error = ex.Message });
            }
        }

        [HttpPost("decrypt-data")]
        public async Task<IActionResult> DecryptData([FromBody] DecryptDataRequest request)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(request.Key);
                aes.IV = Convert.FromBase64String(request.IV);

                var encryptedData = Convert.FromBase64String(request.EncryptedData);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    await cs.WriteAsync(encryptedData, 0, encryptedData.Length);
                    await cs.FlushFinalBlockAsync();
                }

                var decryptedData = Convert.ToBase64String(ms.ToArray());
                return Ok(decryptedData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error decrypting data", error = ex.Message });
            }
        }


        [HttpPost("decrypt-message")]
        public async Task<IActionResult> DecryptMessage([FromBody] DecryptDataRequest request)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(request.Key);
                aes.IV = Convert.FromBase64String(request.IV);

                var encryptedData = Convert.FromBase64String(request.EncryptedData);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    await cs.WriteAsync(encryptedData, 0, encryptedData.Length);
                    await cs.FlushFinalBlockAsync();
                }

                var decryptedBytes = ms.ToArray();
                var decryptedString = Encoding.UTF8.GetString(decryptedBytes);
                
                return Ok(decryptedString);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error decrypting data", error = ex.Message });
            }
        }
    }

    public class DecryptAesKeyRequest
    {
        public string EncryptedAESKey { get; set; }
        public string PrivateKey { get; set; }
    }

    public class DecryptDataRequest
    {
        public string EncryptedData { get; set; }
        public string Key { get; set; }
        public string IV { get; set; }
    }
}