using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

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

            aes.Mode = CipherMode.ECB;

            EncryptMetadata(aes, hashedPswd, filename);
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

        static byte[] ToBytes(int input)
        {
            byte[] intBytes = BitConverter.GetBytes(input);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            byte[] result = intBytes;

            return result;
        }

        // Шифрование метаданных изображения
        static void EncryptMetadata(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesEncryptor = aes.CreateEncryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            // Создаем файл-destination, куда будем записывать зашифрованные данные
            using var fileEncrypt = File.Create(filename + "enc-Metadata.bin");

            // Берем изображение
            Image image = Image.FromFile(filename + ".jpg");

            // Берем метаданные изображения
            PropertyItem[] propItems = image.PropertyItems;

            // Создаем CryptoStream
            using var csEncrypt = new CryptoStream(fileEncrypt, aesEncryptor, CryptoStreamMode.Write);

            
            for (int i = 0; i < propItems.Length; i++)
            {

                csEncrypt.Write(propItems[i].Value);

            }

            csEncrypt.Close();
            fileEncrypt.Close();


            using BinaryReader binaryReader = new BinaryReader(File.OpenRead(filename + "enc-Metadata.bin"));

            for (int i = 0; i < propItems.Length; i++)
            { 

                binaryReader.Read(propItems[i].Value);
                
                image.SetPropertyItem(propItems[i]);
            }

            image.Save(filename + "enc-Metadata.jpg");

            Image SASDAS = Image.FromFile(filename + "enc-Metadata.jpg");

        }

        static void EncryptMetadataIV(Aes aes, byte[] hashedPswd, string filename)
        {
            // Создаем Encryptor
            var aesEncryptor = aes.CreateEncryptor(hashedPswd, hashedPswd.Take(16).ToArray());

            // Создаем файл-destination, куда будем записывать зашифрованные данные
            using var fileEncrypt = File.Create(filename + "enc-Metadata.jpg");

            // Берем изображение
            Image image = Image.FromFile(filename + ".jpg");

            // Берем метаданные изображения
            PropertyItem[] propItems = image.PropertyItems;

            // Создаем CryptoStream
            using var csEncrypt = new CryptoStream(fileEncrypt, aesEncryptor, CryptoStreamMode.Write);

            // Читаем данные с файла-source
            var fileBytes = File.ReadAllBytes(filename + ".jpg");

            // Длина метаданных
            int metadataLen = 0;

            for (int i = 0; i < propItems.Length; i++)
            {
                metadataLen += propItems[i].Len;
            }

            // Записываем данные до метаданных
            fileEncrypt.Write(fileBytes
                                .Take(propItems[0].Id)
                                .ToArray());

            // Шифруем метаданные
            csEncrypt.Write(fileBytes
                                .Skip(propItems[0].Id)
                                .Take(metadataLen)
                                .ToArray());

            // Записываем данные после метаданных
            fileEncrypt.Write(fileBytes
                                .Skip(propItems[0].Id + metadataLen)
                                .Take(fileBytes.Length - metadataLen)
                                .ToArray());

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
