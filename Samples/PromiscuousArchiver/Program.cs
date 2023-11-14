using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DICOMSharp.Network.Workers;
using DICOMSharp.Logging;
using DICOMSharp.Network.Connections;
using DICOMSharp.Data;
using DICOMSharp.Network.QueryRetrieve;
using System.Text.RegularExpressions;

namespace PromiscuousArchiver
{
    class Program
    {
        static List<DICOMData> storedData = new List<DICOMData>();

        static void Main(string[] args)
        {
            DICOMListener listener = new DICOMListener(new ConsoleLogger(), false);
            listener.StoreRequest += new DICOMListener.StoreRequestHandler(listener_StoreRequest);
            listener.AssociationRequest += new DICOMListener.BasicConnectionHandler(listener_AssociationRequest);
            listener.FindRequest += new DICOMListener.QRRequestHandler(listener_FindRequest);

            Console.ReadKey();
        }

        static QRResponseData listener_FindRequest(DICOMConnection conn, QRRequestData request)
        {
            QRResponseData response = request.GenerateResponse();

            foreach (DICOMData data in storedData)
            {
                //check fields
                bool works = true;
                foreach (uint tag in request.SearchTerms.Keys)
                {
                    object search = request.SearchTerms[tag];
                    if (search.ToString() == "")
                        continue;

                    if (!data.Elements.ContainsKey(tag))
                    {
                        works = false;
                        break;
                    }

                    if (search.ToString().Contains("-"))
                    {
                        //range

                        string[] range = search.ToString().Split('-');
                        if (range[0] != "" && data[tag].Display.CompareTo(range[0]) < 0)
                        {
                            works = false;
                            break;
                        }
                        if (range[1] != "" && data[tag].Display.CompareTo(range[1]) > 0)
                        {
                            works = false;
                            break;
                        }
                    }
                    else if (search.ToString().Contains("*"))
                    {
                        if (!Regex.IsMatch(data[tag].Display, search.ToString()))
                        {
                            works = false;
                            break;
                        }
                    }
                    else if (data[tag].Display != search.ToString())
                    {
                        works = false;
                        break;
                    }                        
                }

                if (!works)
                    continue;

                DICOMData ndata = new DICOMData();
                foreach (uint tag in response.TagsToFill)
                {
                    if (data.Elements.ContainsKey(tag))
                        ndata[tag].Data = data[tag].Data;
                }
            }

            return response;
        }

        static void listener_AssociationRequest(DICOMConnection conn)
        {
            //accept anyone!
            conn.SendAssociateAC();
        }

        static void listener_StoreRequest(DICOMConnection conn, DICOMData data)
        {
            //C-Store request with a dataset. cache it...
            storedData.Add(data);
        }
    }
}
