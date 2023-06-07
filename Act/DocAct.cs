using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TN.DocData;

namespace TN.Doc
{
    public class DocAct : DocGeneral
    {
        public DocAct(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Act;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableResultActAndPassportList> ListDoc { get; set; }
        private DbSet<TableResultActAndPassportData> DataDoc { get; set; }


        private Doc Doc;
        private JObject jsonDoc;


        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc
                        where item.End > UTBegin && item.End < UTEnd
                        select item).ToList<TableResultActAndPassportList>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new RequestListDocs()
                    {
                        Id = item.id,
                        //DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.End).ToString("dd.MM.yy HH:mm"),
                        DT = item.strEnd,
                        Description = $"Акт валовый"
                    });
                }
            }

            return docs;
        }

        public override object GetViewDoc(int id)
        {
            var list = (from item in ListDoc
                        where item.id == id
                        select item).First();

            list.Data = (from item in DataDoc
                         where item.id == id
                         select item).First();

            Doc doc = new ();

            LoadCfg(PathToDocConfigFile, ref doc);

            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.strBegin = list.strBegin;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.Begin = list.Begin;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.strEnd = list.strEnd;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.End = list.End;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.Year = list.Year;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.Month = list.Month;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.Day = list.Day;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.PeriodType = list.PeriodType;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.PassportID = list.PassportID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.IsReady = list.IsReady;
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.ResultActAndPassport = JsonDeserializeObject<ResultActAndPassport>(ArrByteToString(list.Data.ResultActAndPassport));
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableResultActAndPassport.TimeStamp = list.TimeStamp;

            jsonDoc = JObject.Parse(JsonSerializeObject<Doc>(doc).ToString());
            Doc = doc;

            return JsonSerializeObject<Doc>(doc);
        }

        public override string GetEditDoc(int id)
        {
            GetViewDoc(id);

            CfgEdit editDoc = new();

            LoadCfg(PathToDocEditConfigFile, ref editDoc);

            var doc = new HtmlDocument();
            doc.Load(PathToRootDirectory + $"/wwwroot/HTML/DocEditAct.html");

            HtmlNode node = doc.GetElementbyId("AdditionalInfo").ChildNodes["tbody"];

            //Рисуем и наполняем таблицу AdditionalInfo
            foreach (var item in editDoc.AdditionalInfo.Where(x => x.Use).ToList())
            {
                string currentValue = "";

                HtmlNode TableRow = HtmlNode.CreateNode("<tr></tr>");
                HtmlNode TableData = HtmlNode.CreateNode("<td></td>");

                TableRow.AppendChild(HtmlNode.CreateNode($"<td>{item.Name}</td>"));
                //TableData.InnerHtml = item.Name;

                if (item.Type == "list")
                {
                    HtmlNode Select = HtmlNode.CreateNode("<select></select>");
                    List<Users> users = new List<Users>();

                    Select.Attributes.Add($"name", $"{item.Key}");
                    Select.Attributes.Add($"data-edit", $"1");
                    Select.Attributes.Add($"data-key", $"{item.Key}");
                    Select.Attributes.Add($"data-tag", $"AdditionalInfo");
                    Select.Attributes.Add($"onchange", $"UserChangeEvent(this)");

                    if (item.Key == "Delive_IOF")
                    {
                        users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 2).ToList();
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_IOF;
                    }
                    else if (item.Key == "Receive_IOF")
                    {
                        users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 3).ToList();
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_IOF;
                    }

                    foreach (var user in users)
                    {
                        HtmlNode Option = HtmlNode.CreateNode("<option></option>");

                        if (user.FIO == currentValue)
                        {
                            //str += $@"<option selected value = {user.Id++}> {user.FIO} </option>" + "\n";

                            Option.Attributes.Add($"selected", $"");
                            Option.Attributes.Add($"value", $"{user.Id++}");
                            Option.InnerHtml = $"{user.FIO}";
                        }
                        else
                        {
                            //str += $@"<option value = {user.Id++}> {user.FIO} </option>" + "\n";

                            Option.Attributes.Add($"value", $"{user.Id++}");
                            Option.InnerHtml = $"{user.FIO}";
                        }

                        Select.AppendChild(Option);
                    }

                    TableData.AppendChild(Select);
                }
                else
                {
                    HtmlNode Input = HtmlNode.CreateNode("<input></input>");

                    if (item.Key == "ActNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.ActNumber;
                    else if (item.Key == "DelivePoint")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.DelivePoint;
                    else if (item.Key == "Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Factory;
                    else if (item.Key == "SIKN_Number")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.SIKN_Number;
                    else if (item.Key == "Delive_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_Factory;
                    else if (item.Key == "Delive_IOF")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_IOF;
                    else if (item.Key == "Delive_Lic_Date")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_Lic_Date;
                    else if (item.Key == "Delive_Lic_Number")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_Lic_Number;
                    else if (item.Key == "Receive_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_Factory;
                    else if (item.Key == "Receive_IOF")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_IOF;
                    else if (item.Key == "Receive_Lic_Date")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_Lic_Date;
                    else if (item.Key == "Receive_Lic_Number")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_Lic_Number;
                    else if (item.Key == "Contract")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Contract;
                    else if (item.Key == "Route_Telegram")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Route_Telegram;
                    else if (item.Key == "Refinery_Plant")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Refinery_Plant;
                    else if (item.Key == "Consignor")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Consignor;
                    else if (item.Key == "Consignee")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Consignee;
                    else if (item.Key == "Destination")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Destination;
                    else if (item.Key == "Exporter_Importer")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Exporter_Importer;
                    else if (item.Key == "Declaration")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Declaration;
                    else if (item.Key == "TankerName")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.TankerName;
                    else if (item.Key == "TimeSchedule_Position")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.TimeSchedule_Position;
                    else if (item.Key == "SubsurfaceUser")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.SubsurfaceUser;
                    else if (item.Key == "FirstOwnerPetroleum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.FirstOwnerPetroleum;
                    else if (item.Key == "LastOwnerPetroleum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.LastOwnerPetroleum;

                    Input.Id = item.Key;
                    Input.Attributes.Add($"data-edit", $"1");
                    Input.Attributes.Add($"data-key", $"{item.Key}");
                    Input.Attributes.Add($"data-tag", $"AdditionalInfo");
                    Input.Attributes.Add($"type", $"{item.Type}");
                    Input.Attributes.Add($"value", $"{currentValue}");

                    if (!item.Edit)
                        Input.Attributes.Add($"disabled", $"disabled");

                    TableData.AppendChild(Input);
                }

                TableRow.AppendChild(TableData);
                doc.GetElementbyId("AdditionalInfo").ChildNodes["tbody"].AppendChild(TableRow);
            }

            doc.Save(PathToRootDirectory + $"/wwwroot/HTML/html.html");

            return "";

            /////--------------------------------
            //GetViewDoc(id);

            //TN.Doc.Edit.CfgEditAct editDoc = new();

            //LoadCfg(PathToDocEditConfigFile, ref editDoc);

            //string str = @"     <style>
            //                    td,th
            //                    {                                    
            //                        font-size: 70%;             /* Размер шрифта в процентах */ 
            //                        padding: 2px;               /* Поля вокруг содержимого таблицы */
            //                        border: 0.5px solid black;  /* Параметры рамки */
            //                    }
            //                    Edit 
            //                    {                                   
            //                        border-collapse: collapse;  /* Убираем двойные линии между ячейками */
            //                    }   
            //                    </style>
                                
            //                    <script type=""text/javascript"">

            //                    function myFunction(selectObject) 
            //                    {" + "\n";

            //str += $"var users = []; " + "\n";
            //foreach (var item in Doc.Doc.Settings.Dictionarys.Users)
            //{
            //    str += @"user = { };";
            //    str += $@"user[""Id""]={item.Id};" + "\n";
            //    str += $@"user[""FIO""]=""{item.FIO}"";" + "\n";
            //    str += $@"user[""Factory""]=""{item.Factory}"";" + "\n";
            //    str += $@"user[""Post""]=""{item.Post}"";" + "\n";
            //    str += @"users.push(user);" + "\n";
            //}

            //str += @"
            //                        if (selectObject.name == ""Delive_IOF"")
            //                        {
            //                            var user = users.filter(users => users[""Id""] == $(selectObject).val());

            //                            document.getElementById(""Delive_Post"").value = user[0].Post;
            //                            document.getElementById(""Delive_Factory"").value = user[0].Factory;
                                        
            //                            //alert(selectObject.name + ' ' + $(selectObject).val() + ' ' + $(selectObject).text());
            //                        }
            //                        else if (selectObject.name == ""Receive_IOF"")
            //                        {
            //                            var user = users.filter(users => users[""Id""] == $(selectObject).val());

            //                            document.getElementById(""Receive_Post"").value = user[0].Post;
            //                            document.getElementById(""Receive_Factory"").value = user[0].Factory;
                                        
            //                            //alert(selectObject.name + ' ' + $(selectObject).val() + ' ' + $(selectObject).text());
            //                        }
            //                    }                                

            //                    </script>" + "\n";

            //str += @"<table id = ""aaa"">
            //         <tbody>" + "\n";

            //foreach (var item in editDoc.AdditionalInfo)
            //{
            //    string currentValue = "";

            //    str += $"<tr>";

            //    str += $"<td>";
            //    str += $@"{item.Name}";
            //    str += $"</td>";

            //    if (item.Type == "list")
            //    {
            //        List<TN.DocData.Users> users = new();

            //        str += $"<td>";
            //        str += $@"<select name=""{item.Key}"" data-edit=""1"" data-key=""{item.Key}"" data-tag=""AdditionalInfo"" onchange=""myFunction(this)"">";

            //        if (item.Key == "Delive_FIO")
            //        {
            //            users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 2).ToList();
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_FIO;
            //        }
            //        else if (item.Key == "Receive_FIO")
            //        {
            //            users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 3).ToList();
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_FIO;
            //        }
            //        else if (item.Key == "Oil_Name")
            //        {
            //            users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 3).ToList();
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Oil_Name;
            //        }


            //        foreach (var user in users)
            //        {
            //            if (user.FIO == currentValue)
            //                str += $@"<option selected value = {user.Id++}> {user.FIO} </option>" + "\n";
            //            else
            //                str += $@"<option value = {user.Id++}> {user.FIO} </option>" + "\n";
            //        }

            //        str += $"</td>";
            //    }
            //    else
            //    {
            //        if (item.Key == "ActNumber")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.ActNumber;
            //        else if (item.Key == "DelivePoint")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.DelivePoint;
            //        else if (item.Key == "Factory")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Factory;
            //        else if (item.Key == "SIKN_Number")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.SIKN_Number;
            //        else if (item.Key == "Delive_Factory")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_Factory;
            //        else if (item.Key == "Delive_IOF")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_IOF;
            //        else if (item.Key == "Delive_Lic_Date")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_Lic_Date;
            //        else if (item.Key == "Delive_Lic_Number")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Delive_Lic_Number;
            //        else if (item.Key == "Receive_Factory")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_Factory;
            //        else if (item.Key == "Receive_IOF")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_IOF;
            //        else if (item.Key == "Receive_Lic_Date")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_Lic_Date;
            //        else if (item.Key == "Receive_Lic_Number")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Receive_Lic_Number;
            //        else if (item.Key == "Contract")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Contract;
            //        else if (item.Key == "Route_Telegram")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Route_Telegram;
            //        else if (item.Key == "Refinery_Plant")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Refinery_Plant;
            //        else if (item.Key == "Consignor")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Consignor;
            //        else if (item.Key == "Consignee")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Consignee;
            //        else if (item.Key == "Destination")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Destination;
            //        else if (item.Key == "Exporter_Importer")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Exporter_Importer;
            //        else if (item.Key == "Declaration")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.Declaration;
            //        else if (item.Key == "TankerName")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.TankerName;
            //        else if (item.Key == "TimeSchedule_Position")
            //            currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo.TimeSchedule_Position;

            //        string disabled = item.Edit ? "" : "disabled";

            //        str += $"<td>";
            //        str += $@"<input id=""{item.Key}"" data-edit=""1"" data-key=""{item.Key}"" data-tag=""AdditionalInfo"" type=""{item.Type}"" value=""{currentValue}"" {disabled}>";
            //        str += $"</td>";
            //    }

            //    str += $"</tr>";
            //}

            //return str;
        }

        public override bool SaveDoc(string jsonData)
        {
            CorrectionData data = JsonDeserializeObject<CorrectionData>(jsonData);

            AdditionalInfo ad = new();
            TableResultActAndPassportData dd = new TableResultActAndPassportData();

            foreach (var item in data.Values)
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    switch (item.Key)
                    {
                        case "ActNumber":
                            ad.ActNumber = item.Value;
                            break;
                        case "DelivePoint":
                            ad.DelivePoint = item.Value;
                            break;
                        case "Factory":
                            ad.Factory = item.Value;
                            break;
                        case "SIKN_Number":
                            ad.SIKN_Number = item.Value;
                            break;
                        case "Delive_Factory":
                            ad.Delive_Factory = item.Value;
                            break;
                        case "Delive_FIO":
                            ad.Delive_FIO = item.Value;
                            break;
                        case "Delive_IOF":
                            ad.Delive_IOF = item.Value;
                            break;
                        case "Delive_Lic_Date":
                            ad.Delive_Lic_Date = item.Value;
                            break;
                        case "Delive_Lic_Number":
                            ad.Delive_Lic_Number = item.Value;
                            break;
                        case "Receive_Factory":
                            ad.Receive_Factory = item.Value;
                            break;
                        case "Receive_FIO":
                            ad.Receive_FIO = item.Value;
                            break;
                        case "Receive_IOF":
                            ad.Receive_IOF = item.Value;
                            break;
                        case "Receive_Lic_Date":
                            ad.Receive_Lic_Date = item.Value;
                            break;
                        case "Receive_Lic_Number":
                            ad.Receive_Lic_Number = item.Value;
                            break;
                        case "Oil_Name":
                            ad.Oil_Name = item.Value;
                            break;
                        case "Contract":
                            ad.Contract = item.Value;
                            break;
                        case "Route_Telegram":
                            ad.Route_Telegram = item.Value;
                            break;
                        case "Refinery_Plant":
                            ad.Refinery_Plant = item.Value;
                            break;
                        case "Consignor":
                            ad.Consignor = item.Value;
                            break;
                        case "Consignee":
                            ad.Consignee = item.Value;
                            break;
                        case "Destination":
                            ad.Destination = item.Value;
                            break;
                        case "Exporter_Importer":
                            ad.Exporter_Importer = item.Value;
                            break;
                        case "Declaration":
                            ad.Declaration = item.Value;
                            break;
                        case "TankerName":
                            ad.TankerName = item.Value;
                            break;
                        case "TimeSchedule_Position":
                            ad.TimeSchedule_Position = item.Value;
                            break;
                        case "SubsurfaceUser":
                            ad.SubsurfaceUser = item.Value;
                            break;
                        case "FirstOwnerPetroleum":
                            ad.FirstOwnerPetroleum = item.Value;
                            break;
                        case "LastOwnerPetroleum":
                            ad.LastOwnerPetroleum = item.Value;
                            break;
                    }
                }
            }


            //ad.ActNumber = data.Values.Where(x => x.Key == "ActNumber").Single().Value;
            //ad.DelivePoint = data.Values.Where(x => x.Key == "DelivePoint").Single().Value;
            //ad.Factory = data.Values.Where(x => x.Key == "Factory").Single().Value;
            //ad.SIKN_Number = data.Values.Where(x => x.Key == "SIKN_Number").Single().Value;
            //ad.Delive_Factory = data.Values.Where(x => x.Key == "Delive_Factory").Single().Value;
            //ad.Delive_FIO = data.Values.Where(x => x.Key == "Delive_FIO").Single().Value;
            //ad.Delive_IOF = data.Values.Where(x => x.Key == "Delive_IOF").Single().Value;
            //ad.Delive_Lic_Date = data.Values.Where(x => x.Key == "Delive_Lic_Date").Single().Value;
            //ad.Delive_Lic_Number = data.Values.Where(x => x.Key == "Delive_Lic_Number").Single().Value;
            //ad.Receive_Factory = data.Values.Where(x => x.Key == "Receive_Factory").Single().Value;
            //ad.Receive_FIO = data.Values.Where(x => x.Key == "Receive_FIO").Single().Value;
            //ad.Receive_IOF = data.Values.Where(x => x.Key == "Receive_IOF").Single().Value;
            //ad.Receive_Lic_Date = data.Values.Where(x => x.Key == "Receive_Lic_Date").Single().Value;
            //ad.Receive_Lic_Number = data.Values.Where(x => x.Key == "Receive_Lic_Number").Single().Value;
            //ad.Oil_Name = data.Values.Where(x => x.Key == "Oil_Name").Single().Value;
            //ad.Contract = data.Values.Where(x => x.Key == "Contract").Single().Value;
            //ad.Route_Telegram = data.Values.Where(x => x.Key == "Route_Telegram").Single().Value;
            //ad.Refinery_Plant = data.Values.Where(x => x.Key == "Refinery_Plant").Single().Value;
            //ad.Consignor = data.Values.Where(x => x.Key == "Consignor").Single().Value;
            //ad.Consignee = data.Values.Where(x => x.Key == "Consignee").Single().Value;
            //ad.Destination = data.Values.Where(x => x.Key == "Destination").Single().Value;
            //ad.Exporter_Importer = data.Values.Where(x => x.Key == "Exporter_Importer").Single().Value;
            //ad.Declaration = data.Values.Where(x => x.Key == "Declaration").Single().Value;
            //ad.TankerName = data.Values.Where(x => x.Key == "TankerName").Single().Value;
            //ad.TimeSchedule_Position = data.Values.Where(x => x.Key == "TimeSchedule_Position").Single().Value;
            //ad.SubsurfaceUser = data.Values.Where(x => x.Key == "SubsurfaceUser").Single().Value;
            //ad.FirstOwnerPetroleum = data.Values.Where(x => x.Key == "FirstOwnerPetroleum").Single().Value;
            //ad.LastOwnerPetroleum = data.Values.Where(x => x.Key == "LastOwnerPetroleum").Single().Value;

            dd.id = data.DocID;
            dd.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(dd).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }

    }

    [Table("TableResultActAndPassport")]
    public class TableResultActAndPassportList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int PeriodType { get; set; }
        public int PassportID { get; set; }
        public int BIK_ID { get; set; }
        public int IsReady { get; set; }
        public long TimeStamp { get; set; }
        public TableResultActAndPassportData Data { get; set; }
    }


    [Table("TableResultActAndPassport")]
    public class TableResultActAndPassportData
    {
        [Key]
        public int id { get; set; }
        public byte[] ResultActAndPassport { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }


    //public class CorrectionData
    //{
    //    public int DocID { get; set; }
    //    public List<EditData> Values { get; set; }
    //}
    //public class EditData
    //{
    //    public string Key { get; set; }
    //    public string Tag { get; set; }
    //    public string Value { get; set; }
    //}


    public class Doc : Root
    {
        public Doc()
        {
            Doc = new();

            Doc.Settings = new Settings
            {
                Header = new HeaderDoc(),
                Data = new DataDoc(),
                Footer = new FooterDoc(),
                Dictionarys = new DictionarysDoc()
            };
            Doc.DataIVK = new DataIVKDoc();
            
            ((DataIVKDoc)Doc.DataIVK).TableResultActAndPassport = new TableResultActAndPassport();
        }
    }

    #region Doc.Settings

    #region Doc.Settings.Header
    public class HeaderDoc : Header
    {

    }

    #endregion

    #region Doc.Settings.Data
    public class DataDoc : Data
    {

    }

    #endregion

    #region Doc.Settings.Footer

    public class FooterDoc : Footer
    {

    }

    #endregion

    #region Doc.Settings.Dictionarys

    public class DictionarysDoc : Dictionarys
    {

    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKDoc : DataIVK
    {
        public TableResultActAndPassport TableResultActAndPassport { get; set; }
    }
    public class TableResultActAndPassport
    {
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int PeriodType { get; set; }
        public int PassportID { get; set; }
        public int BIK_ID { get; set; }
        public int IsReady { get; set; }
        public ResultActAndPassport ResultActAndPassport { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }

        public long TimeStamp { get; set; }
    }
    public class ResultActAndPassport
    {
        public List<Act> Act { get; set; }
        public List<Passport> Passport { get; set; }
        public List<PassportPeriodDT> PassportPeriodDT { get; set; }
        public Common Common { get; set; }
    }
    public class Act
    {
        public string Vol_LastShift { get; set; }
        public string Mass_LastShift { get; set; }
        public string Vol_CurrShift { get; set; }
        public string Mass_CurrShift { get; set; }
        public string Vol_Gross { get; set; }
        public string Mass_Gross { get; set; }
        public string Temp { get; set; }
        public string Press { get; set; }
        public string Density { get; set; }
        public string Density_correction { get; set; }
        public string PassportID { get; set; }
        public Balast Balast { get; set; }
        public string Balast_Mass_Frac { get; set; }
        public string Sulfur { get; set; }
        public string Chloride_Salts { get; set; }
        public string Balast_Mass { get; set; }
        public string Net_Mass { get; set; }
    }
    public class Balast
    {
        public string Water { get; set; }
        public string Chlorides { get; set; }
        public string Impurity { get; set; }
    }
    public class Passport
    {
        public string TempResult { get; set; }
        public string PressResult { get; set; }
        public string DensResult { get; set; }
        public string Dens15Result { get; set; }
        public string Dens20Result { get; set; }
        public string MassWaterFracResult { get; set; }
        public string PassportID { get; set; }
        public ChlorideSalts Chloride_Salts { get; set; }
        public string Impurity { get; set; }
        public string SulfurResult { get; set; }
        public DNP DNP { get; set; }
        public string Yield_fraction_200 { get; set; }
        public string Yield_fraction_300 { get; set; }
        public string Yield_fraction_350 { get; set; }
        public string Mass_fraction_of_paraffin { get; set; }
        public string Mass_fraction_of_hydrogen_sulfide { get; set; }
        public string Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; }
        public string Mass_fraction_of_organic_chlorides { get; set; }
        public string Oil_Type { get; set; }
        public string GOSTIndexResult { get; set; }
        public string ExtendGOSTIndexResult { get; set; }
    }
    public class ChlorideSalts
    {
        public string Concentration { get; set; }
        public string MassFraction { get; set; }
    }
    public class DNP
    {
        public string kPa { get; set; }
        public string mercury_mm { get; set; }
    }
    public class PassportPeriodDT
    {
        public int Begin { get; set; }
        public int End { get; set; }
    }
    public class Common
    {
        public string Begin_Mass { get; set; }
        public string End_Mass { get; set; }
        public string Net_Mass { get; set; }
        public string Balast_Mass { get; set; }
        public string Gross_Mass { get; set; }
        public string Gross_Vol { get; set; }
        public ValueInWords ValueInWords { get; set; }
        public string UsedShift { get; set; }
        public List<string> GostIndex51858 { get; set; }
        public List<string> ExtendGostIndex51858 { get; set; }
    }
    public class ValueInWords
    {
        public string WholePart { get; set; }
        public string Fraction { get; set; }
    }
    public class AdditionalInfo
    {
        public string ActNumber { get; set; }
        public string DelivePoint { get; set; }
        public string Factory { get; set; }
        public string SIKN_Number { get; set; }
        public string Delive_Factory { get; set; }
        public string Delive_FIO { get; set; }
        public string Delive_IOF { get; set; }
        public string Delive_Lic_Date { get; set; }
        public string Delive_Lic_Number { get; set; }
        public string Receive_Factory { get; set; }
        public string Receive_FIO { get; set; }
        public string Receive_IOF { get; set; }
        public string Receive_Lic_Date { get; set; }
        public string Receive_Lic_Number { get; set; }
        public string Oil_Name { get; set; }
        public string Contract { get; set; }
        public string Route_Telegram { get; set; }
        public string Refinery_Plant { get; set; }
        public string Consignor { get; set; }
        public string Consignee { get; set; }
        public string Destination { get; set; }
        public string Exporter_Importer { get; set; }
        public string Declaration { get; set; }
        public string TankerName { get; set; }
        public string TimeSchedule_Position { get; set; }
        public string SubsurfaceUser { get; set; }
        public string FirstOwnerPetroleum { get; set; }
        public string LastOwnerPetroleum { get; set; }
    }

    #endregion


}

//namespace TN.Doc.Edit
//{
//    public class CfgEditAct
//    {
//        public List<AdditionalInfo> AdditionalInfo = new();
//    }
//    public class AdditionalInfo
//    {
//        public int Id { get; set; }
//        public bool Use { get; set; }
//        public bool Edit { get; set; }
//        public string Key { set; get; }
//        public string Type { set; get; }
//        public string Name { set; get; }
//    }
//}