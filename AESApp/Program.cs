using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace AESApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using var aes = Aes.Create();
            
            var hashedPswd = CreateHash("SHA-256");

            string filename = "brigada8";

            aes.Padding = PaddingMode.None;

            EncryptImage(aes, hashedPswd, filename);
            DecryptImage(aes, hashedPswd, filename);
        }

        // Шифрование данных
        static void Encrypt(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesEncryptor = aes.CreateEncryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            // Создаем файл-destination, куда будем записывать зашифрованные данные
            using var fileEncrypt = File.Create(filename + ".bin");

            // Создаем CryptoStream
            using var csEncrypt = new CryptoStream(fileEncrypt, aesEncryptor, CryptoStreamMode.Write);

            // Читаем из файла-source данные и передаем CryptoStream
            csEncrypt.Write(File.ReadAllBytes(filename + ".jpg"));
        }

        // Дешифрование данных
        static void Decrypt(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем декриптор
            var aesDecryptor = aes.CreateDecryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            // Открываем файл-source, с которого будем читать зашифрованные данные
            using var fileDecrypt = File.OpenRead(filename + ".bin");

            // Создаем CryptoStream
            using var csDecrypt = new CryptoStream(fileDecrypt, aesDecryptor, CryptoStreamMode.Read);

            // Создаем файл-destination, куда будем записывать дешифрованные данные
            using var fsWriter = File.Create(filename + "-decr.jpg");

            var buffer = new byte[1024];
            var read = csDecrypt.Read(buffer, 0, buffer.Length);

            while (read > 0)
            {
                fsWriter.Write(buffer, 0, read);
                read = csDecrypt.Read(buffer, 0, buffer.Length);
            }
        }

        // Шифрование данных изображения
        static void EncryptImage(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesEncryptor = aes.CreateEncryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            using Bitmap bmp = new Bitmap(filename + ".png");

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            byte[] data = new byte[Math.Abs(bmpData.Stride * bmpData.Height)];
            Marshal.Copy(bmpData.Scan0, data, 0, data.Length);

            using var msEncrypt = new MemoryStream();

            using (CryptoStream cryptoStream = new CryptoStream(msEncrypt, aesEncryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data);
            }

            data = msEncrypt.ToArray();

            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            bmp.UnlockBits(bmpData);

            bmp.Save(filename + "enc-Data.png", ImageFormat.Png);
        }

        static void DecryptImage(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesDecryptor = aes.CreateDecryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            using Bitmap bmp = new Bitmap(filename + "enc-Data.png");

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            byte[] data = new byte[Math.Abs(bmpData.Stride * bmpData.Height)];
            Marshal.Copy(bmpData.Scan0, data, 0, data.Length);

            using var msEncrypt = new MemoryStream();

            using (CryptoStream cryptoStream = new CryptoStream(msEncrypt, aesDecryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data);
            }

            data = msEncrypt.ToArray();

            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            bmp.UnlockBits(bmpData);

            bmp.Save(filename + "dec-Data.png", ImageFormat.Png);

        }

        static void EncryptDataIV(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesEncryptor = aes.CreateEncryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            // Создаем файл-destination, куда будем записывать зашифрованные данные
            var fileEncrypt = File.Create(filename + "enc-Data.bin");

            // Берем изображение
            using Bitmap bitmap = new Bitmap(filename + ".jpg");

            // Создаем экземпляр класса и генерируем IV
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.GenerateIV();

            // Так как нельзя создать инстанс класса, используемсериализацию
            PropertyItem property = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));

            // Выбираем доступный флаг
            property.Id = 0;

            // Поле, указывающее тип в массиве Value 
            property.Type = 6;

            // Длина массива Value
            property.Len = rijndaelManaged.IV.Length;

            // Сохраняем в Value IV
            property.Value = rijndaelManaged.IV;

            // Ставим новое свойство
            bitmap.SetPropertyItem(property);

            // Создаем CryptoStream
            var csEncrypt = new CryptoStream(fileEncrypt, aesEncryptor, CryptoStreamMode.Write);

            Color color;
            byte[] RGBA = new byte[4];

            // Бежим по изображению
            // Берем пиксель, шифруем RGBA
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    color = bitmap.GetPixel(i, j);
                    RGBA[0] = color.R;
                    RGBA[1] = color.G;
                    RGBA[2] = color.B;
                    RGBA[3] = color.A;
                    csEncrypt.Write(RGBA);
                }
            }

            csEncrypt.Close();
            fileEncrypt.Close();

            BinaryReader binaryReader = new BinaryReader(File.OpenRead(filename + "enc-Data.bin"));

            // Бежим по изображению
            // Сеттим зашифрованные пиксели
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    RGBA = binaryReader.ReadBytes(4);
                    color = Color.FromArgb(RGBA[3], RGBA[0], RGBA[1], RGBA[2]);

                    bitmap.SetPixel(i, j, color);
                }
            }

            binaryReader.Close();

            bitmap.Save(filename + "enc-Data.jpg", bitmap.RawFormat);
            bitmap.Dispose();
        }

        // Вычисление хеша строки
        static byte[] CreateHash(string algorithm)
        {
            // Создаем алгоритм хеширования MD5, SHA-384, SHA-256, invalid - SHA-512
            var hash = HashAlgorithm.Create(algorithm);

            Console.WriteLine("Enter password");

            // Прогоняем пароль через хеш-алгоритм
            byte[] hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(Console.ReadLine()));

            return hashBytes;
        }

    }
}
