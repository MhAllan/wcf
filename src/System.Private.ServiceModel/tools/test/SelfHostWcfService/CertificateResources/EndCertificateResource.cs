﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WcfTestBridgeCommon;

namespace WcfService.CertificateResources
{
    internal abstract class EndCertificateResource : CertificateResource
    {
        public override ResourceResponse Get(ResourceRequestContext context)
        {
            string thumbprint;
            bool thumbprintPresent = context.Properties.TryGetValue(thumbprintKeyName, out thumbprint) && !string.IsNullOrWhiteSpace(thumbprint);

            string subject;
            bool subjectPresent = context.Properties.TryGetValue(subjectKeyName, out subject) && !string.IsNullOrWhiteSpace(subject);

            ResourceResponse response = new ResourceResponse();

            // if no subject and no thumbprint parameter provided, provide a list of certs already PUT to this resource 
            if (!thumbprintPresent && !subjectPresent)
            {
                string retVal = string.Empty;
                string[] subjects;
                string[] thumbprints;

                lock (s_certificateResourceLock)
                {
                    int certNum = s_createdCertsBySubject.Count;
                    subjects = new string[certNum];
                    thumbprints = new string[certNum];

                    foreach (var keyVal in s_createdCertsBySubject)
                    {
                        --certNum;
                        subjects[certNum] = keyVal.Key;
                        thumbprints[certNum] = keyVal.Value.Thumbprint;
                    }
                }

                response.Properties.Add(subjectsKeyName, string.Join(",", subjects));
                response.Properties.Add(thumbprintsKeyName, string.Join(",", thumbprints));
                return response;
            }
            else
            {
                // Otherwise, check on the creation state given the certificate thumbprint or subject
                // thumbprint is given priority if present

                X509Certificate2 certificate = null;
                bool certHasBeenCreated = false;

                lock (s_certificateResourceLock)
                {
                    if (thumbprintPresent)
                    {
                        certHasBeenCreated = s_createdCertsByThumbprint.TryGetValue(thumbprint, out certificate);
                    }
                    else if (subjectPresent)
                    {
                        certHasBeenCreated = s_createdCertsBySubject.TryGetValue(subject, out certificate);
                    }
                }

                if (certHasBeenCreated)
                {
                    response.Properties.Add(thumbprintKeyName, certificate.Thumbprint);
                    response.Properties.Add(certificateKeyName, Convert.ToBase64String(certificate.RawData));
                }
                else
                {
                    response.Properties.Add(thumbprintKeyName, string.Empty);
                    response.Properties.Add(certificateKeyName, string.Empty);
                }
                return response;
            }
        }

        public abstract override ResourceResponse Put(ResourceRequestContext context); 
    }
}
