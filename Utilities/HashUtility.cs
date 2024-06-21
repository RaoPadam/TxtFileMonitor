using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TxtFileMonitor.Utilities
{
        public static class HashUtility
        {
            public static string ComputeHash(byte[] data, int length)
            {
                // Compute MD5 hash for the given data
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(data, 0, length);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }

