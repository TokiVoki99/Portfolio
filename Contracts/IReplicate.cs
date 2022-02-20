﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Contracts
{
    [ServiceContract]
    public interface IReplicate
    {
        [OperationContract]
        byte[] ReadCryptedData();

        [OperationContract]
        string WriteDecryptedData(byte[] encryptedData);

       
    }
}
