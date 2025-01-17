﻿using Contracts;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace ServiceApp
{
    public class DatabaseManagement : IDataBaseManagement, IReplicate
    {

        public static string DataBase = "Database.txt";
        public static string Archive = "Archive.txt";

        //[PrincipalPermission(SecurityAction.Demand, Role = "Admin")]
        public void CreateDatabase()
        {

            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();

            X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Admin")
            {
                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                StreamWriter sw = File.CreateText(DataBase);
                sw = File.CreateText(Archive);
                sw.Close();
                
            }
            else
            {
                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "CreateDatabase method need Admin permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call CreateDB method (time : {1}). " +
                    "For this method need to be member of group Admin.", ime, time.TimeOfDay);

                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }

        }

       // [PrincipalPermission(SecurityAction.Demand, Role = "Admin")]
        public bool ArchiveDatabase()
        {

            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();

            X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Admin")
            {

                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                if (File.Exists(DataBase) && File.Exists(Archive))
                {
                    Dictionary<int, Consumer> cons = new Dictionary<int, Consumer>();
                    cons = ReadDB(DataBase);
                    return OverwriteDB(cons, Archive);
                }
                else
                {
                    Console.WriteLine("Database and archive not created");
                    return false;
                }
            }
            else
            {
                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "ArchiveDatabase method need Admin permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call ArchiveDB method (time : {1}). " +
                    "For this method need to be member of group Admin.", ime, time.TimeOfDay);

                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }
        }

       // [PrincipalPermission(SecurityAction.Demand, Role = "Admin")]
        public void DeleteDatabase()
        {

            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();

            X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Admin")
            {
                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                if (File.Exists(DataBase))
                {
                    File.Delete(DataBase);
                    Console.WriteLine("Database deleted");
                }
                else
                {
                    Console.WriteLine("Database doesnt exist!");
                }

                if (File.Exists(Archive))
                {
                    File.Delete(Archive);
                    Console.WriteLine("Archive deleted");
                }
                else
                {
                    Console.WriteLine("Archive doesn't exist");
                }
            }
            else
            {
                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "DeleteDatabase method need Admin permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call DeleteDB method (time : {1}). " +
                    "For this method need to be member of group Admin.", ime, time.TimeOfDay);

                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }
        }

     
        public Consumer FindMaxInRegion(string region, out int id)
        {
            List<int> lista = new List<int>() {0,0,0,0,0,0,0,0,0,0,0,0 };
            Consumer consumer = new Consumer("Nista", "Nista",1992, lista);
            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();
           
             X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Reader")
            {
                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                int suma = 0;
                int max = 0;
                id  = 0;
                Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
                consumers = ReadDB(DataBase);
                foreach (var cons in consumers)
                {
                    if (cons.Value.Region == region)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            suma += cons.Value.Amount[i];

                        }
                        if (suma > max)
                        {
                            max = suma;
                            id = cons.Key;

                        }
                    }
                    suma = 0;
                }
              
                if (id == 0)
                {
                    try
                    {
                        Audit.FindMaxInRegionFailure(ime, "No consumer from that region");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    return consumer;
                }
                else
                {
                    try
                    {
                        Audit.FindMaxInRegionSuccess(ime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    return consumers[id];
                }
              
            }
            else
            {
                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "FindMaxInRegion method need Reader permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call FindMax method (time : {1}). " +
                    "For this method need to be member of group Reader.", ime, time.TimeOfDay);
              
                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }

            


        }

       
        public float MeanValByCity(string city)
        {

            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();

            X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Reader")
            {

                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
                int cnt = 0;
                float suma = 0;
                consumers = ReadDB(DataBase);
                foreach (var cons in consumers.Values)
                {
                    if (cons.City == city)
                    {
                        cnt++;
                        for (int i = 0; i < 12; i++)
                        {
                            suma += cons.Amount[i];

                        }
                    }

                }
                if(suma / cnt != 0)
                {
                    try
                    {
                        Audit.MeanValByCitySuccess(ime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    try
                    {
                        Audit.MeanValByCityFailure(ime, "No consumer from that city");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                return suma / cnt;
            }
            else
            {
                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "MeanValByCity method need Reader permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call MeanValByCity method (time : {1}). " +
                    "For this method need to be member of group Reader.", ime, time.TimeOfDay);

                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }
        }

        
        public float MeanValByRegion(string region)
        {

            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();

            X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Reader")
            {
                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
                float suma = 0;
                int cnt = 0;
                float meanVal = 0;
                consumers = ReadDB(DataBase);
                foreach (var cons in consumers.Values)
                {
                    if (cons.Region == region)
                    {
                        cnt++;
                        for (int i = 0; i < 12; i++)
                        {
                            suma += cons.Amount[i];

                        }
                    }

                }
                meanVal = suma / cnt;
                if(meanVal == 0)
                {
                    try
                    {
                        Audit.MeanValByRegionFalure(ime, "No consumer in that region");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    try
                    {
                        Audit.MeanValByRegionSuccess(ime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                return suma / cnt;
            }
            else
            {
                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "MeanValByRegion method need Reader permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call MeanValByRegion method (time : {1}). " +
                    "For this method need to be member of group Reader.", ime, time.TimeOfDay);

                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }
        }

        //[PrincipalPermission(SecurityAction.Demand, Role = "Modifier")]
        public bool Modify(int id, Consumer consumer)
        {

            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();

            X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Modifier")
            {
                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
                consumers = ReadDB(DataBase);
                if (consumers.ContainsKey(id))
                {
                    consumers[id] = consumer;
                    try
                    {
                        Audit.ModifySuccess(ime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    return OverwriteDB(consumers, DataBase);
                }
                try
                {
                    Audit.ModifyFailure(ime, "Consumer with that ID already exists");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                return false;
            }
            else
            {
                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "Modify method need Modifier permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call Modify method (time : {1}). " +
                    "For this method need to be member of group Modifier.", ime, time.TimeOfDay);

                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }
        }


        //[PrincipalPermission(SecurityAction.Demand, Role = "Modifier")]
        public bool Write(int id, Consumer consumer)
        {
            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();

            X509Certificate2 clientCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, ime);

            string uloga = CertManager.GetClientRole(clientCert);
            if (uloga == "Modifier")
            {
                try
                {
                    Audit.AuthorizationSuccess(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
                consumers = ReadDB(DataBase);
                if (consumers.ContainsKey(id))
                {
                    try
                    {
                        Audit.WriteFailure(ime,"Consumer with that ID already exists");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    return false;
                }
                else
                {
                    consumers.Add(id, consumer);
                    try
                    {
                        Audit.WriteSuccess(ime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    return OverwriteDB(consumers, DataBase);
                }
            }
            else
            {

                try
                {
                    Audit.AuthorizationFailed(ime,
                        OperationContext.Current.IncomingMessageHeaders.Action, "Write method needs Modifier permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                DateTime time = DateTime.Now;
                string message = String.Format("Access is denied. User {0} try to call Write method (time : {1}). " +
                    "For this method need to be member of group Modifier.", ime, time.TimeOfDay);

                Console.WriteLine(message);
                throw new FaultException<SecurityException>(new SecurityException(message));
            }
        }

        public bool OverwriteDB(Dictionary<int,Consumer> cons, string filename)
        {

            
            
                List<string> output = new List<string>();
                string outStr;
                string monthStr;
                foreach (var item in cons)
                {
                    outStr = "";
                    monthStr = "";
                    foreach (var month in item.Value.Amount)
                    {
                        monthStr += ',' + month.ToString();
                    }
                    outStr = $"{item.Key},{item.Value.Region},{item.Value.City},{item.Value.Year}" + monthStr;
                    output.Add(outStr);
                }
                if (File.Exists(DataBase))
                {
                    File.WriteAllLines(filename, output);
                    return true;
                }
                return false;
            
        }

        public Dictionary<int, Consumer> ReadDB(string filename)
        {
            Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
            try
            {
                List<string> lines = File.ReadAllLines(filename).ToList();
                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                    string[] entries = line.Split(',');

                    Consumer cons = new Consumer();

                    
                    cons.Region = entries[1];
                    cons.City = entries[2];
                    cons.Year = Int32.Parse(entries[3]);
                   
                    Console.WriteLine(cons.Region);
                    for (int i = 0; i < 12; i++)
                    {
                        cons.Amount.Add(Int32.Parse(entries[i + 4]));
                       
                    }
                    
                    consumers.Add(Int32.Parse(entries[0]), cons);
                    
                    
                }
               
               
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return consumers;
        }

       
        public void TestCommunication()
        {
            Console.WriteLine("Communication established by:{0}", WindowsIdentity.GetCurrent().Name);
            string ime = CertManager.Parse(ServiceSecurityContext.Current.PrimaryIdentity.Name, "CN").FirstOrDefault();
            try
            {
                Audit.AuthenticationSuccess(ime);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public byte[] ReadCryptedData()
        {
            byte[] encrypted;
            byte[] IV;
            string plainText = File.ReadAllText(DataBase);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = ASCIIEncoding.ASCII.GetBytes(LoadKey("SecretKey.txt"));

                aesAlg.GenerateIV();
                IV = aesAlg.IV;

                aesAlg.Mode = CipherMode.CBC;

                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption. 
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            var combinedIvCt = new byte[IV.Length + encrypted.Length];
            Array.Copy(IV, 0, combinedIvCt, 0, IV.Length);
            Array.Copy(encrypted, 0, combinedIvCt, IV.Length, encrypted.Length);

            // Return the encrypted bytes from the memory stream. 
            return combinedIvCt;
        }

        public static string LoadKey(string inFile)
        {
            FileStream fInput = new FileStream(inFile, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[(int)fInput.Length];

            try
            {
                fInput.Read(buffer, 0, (int)fInput.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("SecretKeys.LoadKey:: ERROR {0}", e.Message);
            }
            finally
            {
                fInput.Close();
            }

            return ASCIIEncoding.ASCII.GetString(buffer);
        }

        public string WriteDecryptedData(byte[] encryptedData)
        {
            string plaintext = null;

            // Create an Aes object 
            // with the specified key and IV. 
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = ASCIIEncoding.ASCII.GetBytes(LoadKey("SecretKey.txt"));

                byte[] IV = new byte[aesAlg.BlockSize / 8];
                byte[] cipherText = new byte[encryptedData.Length - IV.Length];

                Array.Copy(encryptedData, IV, IV.Length);
                Array.Copy(encryptedData, IV.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = IV;

                aesAlg.Mode = CipherMode.CBC;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption. 
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }
            return plaintext;
            

        }


        public bool CheckID(int id)
        {
            Dictionary<int, Consumer> consumers = ReadDB(DataBase);
            if (consumers.ContainsKey(id))
            {
                return true;
            }
            return false;
        }
    }
}
