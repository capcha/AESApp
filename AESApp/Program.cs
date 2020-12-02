using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;

namespace AESApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using var aes = Aes.Create();
            
            var hashedPswd = CreateHash("SHA-256");

            string filename = "brigada8";

            Encrypt(aes, hashedPswd, filename);

            Decrypt(aes, hashedPswd, filename);

            aes.Mode = CipherMode.CBC;

            EncryptData(aes, hashedPswd, filename);
            EncryptDataIV(aes, hashedPswd, filename);

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

        // Шифрование метаданных изображения
        static void EncryptData(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesEncryptor = aes.CreateEncryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            // Создаем файл-destination, куда будем записывать зашифрованные данные
            var fileEncrypt = File.Create(filename + "enc-Data.bin");

            // Берем изображение
            using Bitmap bitmap = new Bitmap(filename + ".jpg");

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

                    bitmap.SetPixel(i, j, Color.FromArgb(RGBA[3], RGBA[0], RGBA[1], RGBA[2]));
                }
            }
            
            binaryReader.Close();
            File.Delete(filename + "enc-Data.bin");
            // Сохраняем
            bitmap.Save(filename + "enc-Data.jpg", bitmap.RawFormat);

        }

        static void DecryptData(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesDecryptor = aes.CreateDecryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            // Берем изображение
            using Bitmap bitmap = new Bitmap(filename + "enc-Data.jpg");

            Color color;
            byte[] RGBA = new byte[4];

            var fileEncrypt = File.Create(filename + "enc-Data.bin");

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

                    fileEncrypt.Write(RGBA);
                }
            }

            fileEncrypt.Close();

            // Создаем файл-source, откуда будем брать зашифрованные данные
            var fileDecrypt = File.OpenRead(filename + "enc-Data.bin");
            // Создаем CryptoStream
            var csEncrypt = new CryptoStream(fileDecrypt, aesDecryptor, CryptoStreamMode.Read);

            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    csEncrypt.Read(RGBA);

                    bitmap.SetPixel(i, j, Color.FromArgb(RGBA[3], RGBA[0], RGBA[1], RGBA[2]));
                }
            }

            fileDecrypt.Close();
            csEncrypt.Close();

            File.Delete(filename + "enc-Data.bin");

            // Сохраняем
            bitmap.Save(filename + "decr-Data.jpg", bitmap.RawFormat);

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
            using RijndaelManaged rijndaelManaged = new RijndaelManaged();
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

            using BinaryReader binaryReader = new BinaryReader(File.OpenRead(filename + "enc-Data.bin"));

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

            bitmap.Save(filename + "enc-Data.jpg", bitmap.RawFormat);
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
