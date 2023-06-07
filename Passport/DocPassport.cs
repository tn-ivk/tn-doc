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
    public class DocPassport : DocGeneral
    {
        public DocPassport(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Passport;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableActAndPassportList> ListDoc { get; set; }
        private DbSet<TableActAndPassportData> DataDoc { get; set; }
        private DbSet<FillingTableActAndPassport> FillingDataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc
                        where item.End > UTBegin && item.End < UTEnd
                        select item).ToList<TableActAndPassportList>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new RequestListDocs()
                    {
                        Id = item.id,
                        //DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.End).ToString("dd.MM.yy HH:mm"),
                        DT = item.strEnd,
                        Description = $"Смена {item.Period}"
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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.StrBegin = list.strBegin;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.Begin = list.Begin;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.strEnd = list.strEnd;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.End = list.End;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.PeriodType = list.PeriodType;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.Period = list.Period;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.IsFilled = list.IsFilled;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.TableActAndPassport = JsonDeserializeObject<TableActAndPassport>(ArrByteToString(list.Data.ActAndPassport));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.AdditionalData = JsonDeserializeObject<AdditionalData>(ArrByteToString(list.Data.AdditionalData));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.PassportResult = JsonDeserializeObject<PassportResult>(ArrByteToString(list.Data.PassportResult));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.TimeStamp = list.TimeStamp;
            if (!string.IsNullOrEmpty(list.Data.DataARM))
                ((DataIVKDoc)doc.Doc.DataIVK).TablePassport.DataARM = JsonDeserializeObject<DataARM>(list.Data.DataARM);
            
            jsonDoc = JObject.Parse(JsonSerializeObject<Doc>(doc).ToString());
            Doc = doc;

            return JsonSerializeObject<Doc>(doc);
        }
        public override string GetEditDoc(int id)
        {
            GetViewDoc(id);

            TN.Doc.Edit.CfgEditPassport editDoc = new();

            LoadCfg(PathToDocEditConfigFile, ref editDoc);

            var doc = new HtmlDocument();
            doc.Load(PathToRootDirectory + $"/wwwroot/HTML/DocEditPassport.html");

            HtmlNode node = doc.GetElementbyId("AdditionalInfo").ChildNodes["tbody"];

            //Рисуем и наполняем таблицу AdditionalInfo
            foreach (var item in editDoc.AdditionalInfo.Where(x=>x.Use).ToList())
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

                    if (item.Key == "Laboratory_IOF")
                    {
                        users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 1).ToList();
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Laboratory_IOF;
                    }
                    else if (item.Key == "Delive_IOF")
                    {
                        users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 2).ToList();
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Delive_IOF;
                    }
                    else if (item.Key == "Receive_IOF")
                    {
                        users = Doc.Doc.Settings.Dictionarys.Users.Where(x => x.IdGroup == 3).ToList();
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Receive_IOF;
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
                else if (item.Type == "datetime-local")
                {
                    HtmlNode Input = HtmlNode.CreateNode("<input></input>");
                    DateTime dt = new();

                    if (item.Key == "PassportPeriodDT.Begin")
                        dt = UnixTimestampToDatetime(((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.PassportPeriodDt.Begin);
                    else if (item.Key == "PassportPeriodDT.End")
                        dt = UnixTimestampToDatetime(((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.PassportPeriodDt.End);

                    Input.Id = item.Key;
                    Input.Attributes.Add($"data-edit", $"1");
                    Input.Attributes.Add($"data-key", $"{item.Key}");
                    Input.Attributes.Add($"data-tag", $"AdditionalInfo");
                    Input.Attributes.Add($"type", $"{item.Type}");
                    Input.Attributes.Add($"value", $"{dt.ToString("s")}");

                    TableData.AppendChild(Input);
                }
                else
                {
                    HtmlNode Input = HtmlNode.CreateNode("<input></input>");

                    if (item.Key == "Passport.PassportID")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.TableActAndPassport.Passport.PassportId;
                    else if (item.Key == "DelivePoint")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.DelivePoint;
                    else if (item.Key == "AccrSertifNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.AccrSertifNumber;
                    else if (item.Key == "Laboratory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Laboratory;
                    else if (item.Key == "SIKN_Number")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.SIKN_Number;
                    else if (item.Key == "Laboratory_Post")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Laboratory_Post;
                    else if (item.Key == "Laboratory_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Laboratory_Factory;
                    else if (item.Key == "Delive_Post")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Delive_Post;
                    else if (item.Key == "Delive_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Delive_Factory;
                    else if (item.Key == "Receive_Post")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Receive_Post;
                    else if (item.Key == "Receive_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.Receive_Factory;
                    else if (item.Key == "TankerName")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.AdditionalData.TankerName;                    
                    else if (item.Key == "ExportPermit")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.DataARM.ExportPermit;
                    else if (item.Key == "Sample")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.DataARM.Sample;

                    Input.Id = item.Key;
                    Input.Attributes.Add($"data-edit", $"1");
                    Input.Attributes.Add($"data-key", $"{item.Key}");
                    Input.Attributes.Add($"data-tag", $"AdditionalInfo");
                    Input.Attributes.Add($"type", $"{item.Type}");
                    Input.Attributes.Add($"value", $"{currentValue}");

                    TableData.AppendChild(Input);
                }

                TableRow.AppendChild(TableData);
                doc.GetElementbyId("AdditionalInfo").ChildNodes["tbody"].AppendChild(TableRow);
            }

            //Рисуем и наполняем таблицу Edit
            int index = 1;
            foreach (var item in editDoc.Parameters.Where(x=>x.Use).ToList())
            {
                string metodName = jsonDoc.SelectTokens($"Doc.DataIVK.TablePassport.TableActAndPassport.Passport.{item.Key.Replace("Correction", "Result")}.Desc").Single().ToString();
                string paramValue = jsonDoc.SelectTokens($"Doc.DataIVK.TablePassport.PassportResult.{item.Key.Replace("Correction", "Result")}").Single().ToString();
                
                HtmlNode TableRow = HtmlNode.CreateNode("<tr></tr>");
                HtmlNode TableDataSelect = HtmlNode.CreateNode("<td></td>");
                HtmlNode TableDataInput = HtmlNode.CreateNode("<td></td>");

                //Номер
                TableRow.AppendChild(HtmlNode.CreateNode($"<td>{index++}</td>"));
                
                //Название параметра
                TableRow.AppendChild(HtmlNode.CreateNode($"<td>{item.Name}</td>"));

                //Метод испытаний
                HtmlNode Select = HtmlNode.CreateNode("<select></select>");

                Select.Attributes.Add($"data-edit", $"1");
                Select.Attributes.Add($"data-key", $"{item.Key}");
                Select.Attributes.Add($"data-tag", $"Metod");
                Select.Attributes.Add($"name", $"cb{item.Id}");

                var list = editDoc.Metods.Where(x => x.IdParameter == item.Id).ToList();

                int i = 0;
                foreach (var metod in list)
                {
                    HtmlNode Option = HtmlNode.CreateNode("<option></option>");

                    if (metod.Name == metodName)
                    {
                        //str += $@"<option selected value = {i++}> {l.Name} </option>" + "\n";
                        
                        Option.Attributes.Add($"selected", $"");
                        Option.Attributes.Add($"value", $"{i++}");
                        Option.InnerHtml = $"{metod.Name}";

                    }
                    else
                    {
                        //str += $@"<option value = {i++}> {l.Name} </option>" + "\n";

                        Option.Attributes.Add($"value", $"{i++}");
                        Option.InnerHtml = $"{metod.Name}";

                    }
                    Select.AppendChild(Option);
                }

                TableDataSelect.AppendChild(Select);
                TableRow.AppendChild(TableDataSelect);

                //Значение ИВК
                TableRow.AppendChild(HtmlNode.CreateNode("<td></td>"));

                //Значение ХАЛ
                HtmlNode Input = HtmlNode.CreateNode("<input></input>");
                Input.Attributes.Add($"data-edit", $"1");
                Input.Attributes.Add($"data-key", $"{item.Key}");
                Input.Attributes.Add($"data-tag", $"Value");
                Input.Attributes.Add($"type", $"number");
                Input.Attributes.Add($"name", $"tb{item.Id}");

                TableDataInput.AppendChild(Input);
                TableRow.AppendChild(TableDataInput);

                //Результат
                TableRow.AppendChild(HtmlNode.CreateNode($"<td>{paramValue}</td>"));

                doc.GetElementbyId("Edit").ChildNodes["tbody"].AppendChild(TableRow);
            }
            
            doc.Save(PathToRootDirectory + $"/wwwroot/HTML/html.html");

            return "";
        }
        public override bool SaveDoc(string jsonData)
        {
            var data = JsonDeserializeObject<CorrectionData>(jsonData);

            FillingTableActAndPassport dd = new();
            TableActAndPassportData tableActAndPassportData = new ();
            TN.Doc.Edit.Correction corr = new();
            TN.Doc.Edit.AdditionalData ad = new();
            DataARM dataARM = new ();
            //string dt;

            foreach (var item in data.Values.Where(x => x.Tag == "AdditionalInfo"))
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    switch (item.Key)
                    {
                        case "Laboratory_Factory": 
                            ad.Laboratory_Factory = item.Value;
                            break;
                        case "AccrSertifNumber":
                            ad.AccrSertifNumber = item.Value;
                            break;
                        case "DelivePoint":
                            ad.DelivePoint = item.Value;
                            break;
                        case "Delive_Factory":
                            ad.Delive_Factory = item.Value;
                            break;
                        case "Delive_IOF":
                            ad.Delive_IOF = item.Value;
                            break;
                        case "Delive_Post":
                            ad.Delive_Post = item.Value;
                            break;
                        case "Laboratory":
                            ad.Laboratory = item.Value;
                            break;
                        case "Laboratory_IOF":
                            ad.Laboratory_IOF = item.Value;
                            break;
                        case "Laboratory_Post":
                            ad.Laboratory_Post = item.Value;
                            break;
                        case "PassportPeriodDT.Begin":
                            ad.PassportPeriodDT.Begin = DatetimeToUnixTimestamp(DateTime.Parse(item.Value));
                            break;
                        case "PassportPeriodDT.End":
                            ad.PassportPeriodDT.End = DatetimeToUnixTimestamp(DateTime.Parse(item.Value)); 
                            break;
                        case "Receive_Factory":
                            ad.Receive_Factory = item.Value;
                            break;
                        case "Receive_IOF":
                            ad.Receive_IOF = item.Value;
                            break;
                        case "Receive_Post":
                            ad.Receive_Post = item.Value;
                            break;
                        case "SIKN_Number":
                            ad.SIKN_Number = item.Value;
                            break;
                        case "TankerName":
                            ad.TankerName = item.Value;
                            break;
                        case "ExportPermit":
                            dataARM.ExportPermit = item.Value;
                            break;
                        case "Sample":
                            dataARM.Sample = item.Value;
                            break;
                    }
                }
            }

            //ad.Laboratory_Factory = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Laboratory_Factory").Single().Value;
            //ad.AccrSertifNumber = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "AccrSertifNumber").Single().Value;
            //ad.DelivePoint = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "DelivePoint").Single().Value;
            //ad.Delive_Factory = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Delive_Factory").Single().Value;
            //ad.Delive_IOF = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Delive_IOF").Single().Value;
            //ad.Delive_Post = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Delive_Post").Single().Value;
            //ad.Laboratory = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Laboratory").Single().Value;
            ////ad.Laboratory_Factory = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Laboratory_Factory").Single().Value;
            //ad.Laboratory_IOF = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Laboratory_IOF").Single().Value;
            //ad.Laboratory_Post = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Laboratory_Post").Single().Value;

            //dt = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "PassportPeriodDT.Begin").Single().Value;
            //ad.PassportPeriodDT.Begin = DatetimeToUnixTimestamp(DateTime.Parse(dt));

            //dt = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "PassportPeriodDT.End").Single().Value;
            //ad.PassportPeriodDT.End = DatetimeToUnixTimestamp(DateTime.Parse(dt));

            //ad.Receive_Factory = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Receive_Factory").Single().Value;
            //ad.Receive_IOF = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Receive_IOF").Single().Value;
            //ad.Receive_Post = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Receive_Post").Single().Value;
            //ad.SIKN_Number = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "SIKN_Number").Single().Value;
            //ad.TankerName = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "TankerName").Single().Value;

            corr.PassportID = data.Values.Where(x => x.Tag == "AdditionalInfo" && x.Key == "Passport.PassportID").Single().Value;

            foreach (var item in data.Values.Where(x => x.Tag == "Value"))
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    switch (item.Key)
                    {
                        case "TempCorrection":
                            corr.TempCorrection.Data.Value = StringToFloat(item.Value);
                            corr.TempCorrection.Data.Legal = 1;
                            corr.TempCorrection.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;

                        case "PressCorrection":
                            corr.PressCorrection.Data.Value = StringToFloat(item.Value);
                            corr.PressCorrection.Data.Legal = 1;
                            corr.PressCorrection.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "DensCorrection":
                            corr.DensCorrection.Data.Value = StringToFloat(item.Value);
                            corr.DensCorrection.Data.Legal = 1;
                            corr.DensCorrection.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Dens15Correction":
                            corr.Dens15Correction.Data.Value = StringToFloat(item.Value);
                            corr.Dens15Correction.Data.Legal = 1;
                            corr.Dens15Correction.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Dens20Correction":
                            corr.Dens20Correction.Data.Value = StringToFloat(item.Value);
                            corr.Dens20Correction.Data.Legal = 1;
                            corr.Dens20Correction.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "MassWaterFracCorrection":
                            corr.MassWaterFracCorrection.Data.Value = StringToFloat(item.Value);
                            corr.MassWaterFracCorrection.Data.Legal = 1;
                            corr.MassWaterFracCorrection.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Chloride_Salts.Concentration":
                            corr.Chloride_Salts.Concentration.Data.Value = StringToFloat(item.Value);
                            corr.Chloride_Salts.Concentration.Data.Legal = 1;
                            corr.Chloride_Salts.Concentration.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Chloride_Salts.MassFraction":
                            corr.Chloride_Salts.MassFraction.Data.Value = StringToFloat(item.Value);
                            corr.Chloride_Salts.MassFraction.Data.Legal = 1;
                            corr.Chloride_Salts.MassFraction.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Impurity":
                            corr.Impurity.Data.Value = StringToFloat(item.Value);
                            corr.Impurity.Data.Legal = 1;
                            corr.Impurity.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "SulfurCorrection":
                            corr.SulfurCorrection.Data.Value = StringToFloat(item.Value);
                            corr.SulfurCorrection.Data.Legal = 1;
                            corr.SulfurCorrection.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "DNP.kPa":
                            corr.DNP.kPa.Data.Value = StringToFloat(item.Value);
                            corr.DNP.kPa.Data.Legal = 1;
                            corr.DNP.kPa.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "DNP.mercury_mm":
                            corr.DNP.mercury_mm.Data.Value = StringToFloat(item.Value);
                            corr.DNP.mercury_mm.Data.Legal = 1;
                            corr.DNP.mercury_mm.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Yield_fraction_200":
                            corr.Yield_fraction_200.Data.Value = StringToFloat(item.Value);
                            corr.Yield_fraction_200.Data.Legal = 1;
                            corr.Yield_fraction_200.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Yield_fraction_300":
                            corr.Yield_fraction_300.Data.Value = StringToFloat(item.Value);
                            corr.Yield_fraction_300.Data.Legal = 1;
                            corr.Yield_fraction_300.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Yield_fraction_350":
                            corr.Yield_fraction_350.Data.Value = StringToFloat(item.Value);
                            corr.Yield_fraction_350.Data.Legal = 1;
                            corr.Yield_fraction_350.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Mass_fraction_of_paraffin":
                            corr.Mass_fraction_of_paraffin.Data.Value = StringToFloat(item.Value);
                            corr.Mass_fraction_of_paraffin.Data.Legal = 1;
                            corr.Mass_fraction_of_paraffin.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Mass_fraction_of_hydrogen_sulfide":
                            corr.Mass_fraction_of_hydrogen_sulfide.Data.Value = StringToFloat(item.Value);
                            corr.Mass_fraction_of_hydrogen_sulfide.Data.Legal = 1;
                            corr.Mass_fraction_of_hydrogen_sulfide.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Mass_fraction_of_methyl_and_ethyl_mercaptan":
                            corr.Mass_fraction_of_methyl_and_ethyl_mercaptan.Data.Value = StringToFloat(item.Value);
                            corr.Mass_fraction_of_methyl_and_ethyl_mercaptan.Data.Legal = 1;
                            corr.Mass_fraction_of_methyl_and_ethyl_mercaptan.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                        case "Mass_fraction_of_organic_chlorides":
                            corr.Mass_fraction_of_organic_chlorides.Data.Value = StringToFloat(item.Value);
                            corr.Mass_fraction_of_organic_chlorides.Data.Legal = 1;
                            corr.Mass_fraction_of_organic_chlorides.Desc = data.Values.Where(x => x.Tag == "Metod" && x.Key == item.Key).Single().Value;
                            break;
                    }
                }
            }

            tableActAndPassportData.id = data.DocID;
            tableActAndPassportData.DataARM = JsonSerializeObject(dataARM).ToString();

            this.Entry(tableActAndPassportData).Property(x => x.DataARM).IsModified = true;

            dd.id = 1;
            dd.PassportID = data.DocID;
            dd.Data = StringToHexArrByte(corr);
            dd.AdditionalData = StringToHexArrByte(ad);

            FillingDataDoc.Update(dd);

            this.SaveChanges();

            return true;
        }
    }

    [Table("TableActAndPassportData")]
    public class FillingTableActAndPassport
    {
        public int id { get; set; }
        public int PassportID { get; set; }
        public byte[] Data { get; set; }
        public byte[] AdditionalData { get; set; }
    }
    
    [Table("TableActAndPassport")]
    public class TableActAndPassportList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int PeriodType { get; set; }
        public int Period { get; set; }
        public int BIK_ID { get; set; }
        public int IsFilled { get; set; }
        public long TimeStamp { get; set; }
        public TableActAndPassportData Data { get; set; }
    }
    
    [Table("TableActAndPassport")]
    public class TableActAndPassportData
    {
        [Key]
        public int id { get; set; }
        public byte[] ActAndPassport { get; set; }
        public byte[] AdditionalData { get; set; }
        public byte[] PassportResult { get; set; }
        public string DataARM { get; set; }
    }

    public class CorrectionData
    {
        public int DocID { get; set; }
        public List<EditData> Values { get; set; }
    }
    public class EditData
    {
        public string Key { get; set; }
        public string Tag { get; set; }
        public string Value { get; set; }
    }

    public class Doc : Root
    {
        public Doc()
        {
            Doc = new()
            {
                Settings = new Settings()
                {
                    Header = new HeaderDoc(),
                    Data = new DataDoc(),
                    Footer = new FooterDoc(),
                    Dictionarys = new DictionarysDoc()
                }
            };
            Doc.DataIVK = new DataIVKDoc();
            ((DataIVKDoc)Doc.DataIVK).TablePassport = new TablePassport();
        }
    }

    #region Doc.Settings

    #region Doc.Settings.Header
    public class HeaderDoc : Header
    {
        public string FieldSIKN { get; set; }
    }

    #endregion

    #region Doc.Settings.Data
    public class DataDoc : TN.DocData.Data
    {
        public List<Parameter> Parameters { get; set; }
    }
    public partial class Parameter
    {
        public bool Use { get; set; }
        public bool Edit { get; set; }
        public string Name { get; set; }
        public string SI { get; set; }
        public string KeyPassportResult { get; set; }
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
        public TablePassport TablePassport { get; set; }
    }
    public partial class TablePassport
    {
        public long id { get; set; }
        public string StrBegin { get; set; }
        public long Begin { get; set; }
        public string strEnd { get; set; }
        public long End { get; set; }
        public long PeriodType { get; set; }
        public long Period { get; set; }
        public long BIK_ID { get; set; }
        public long IsFilled { get; set; }
        public TableActAndPassport TableActAndPassport { get; set; }
        public AdditionalData AdditionalData { get; set; }
        public PassportResult PassportResult { get; set; }
        public long TimeStamp { get; set; }
        public DataARM DataARM { get; set; } = new DataARM();
    }
    public partial class TableActAndPassport
    {
        public Act Act { get; set; }
        public Passport Passport { get; set; }
    }
    public partial class Act
    {
        public Data Mass_Gross_Raw { get; set; }
    }
    public class Data
    {
        public float Value { get; set; } = 0;
        public int Legal { get; set; } = 0;
    }
    public partial class Passport
    {
        public ParameterProperties TempResult { get; set; }
        public ParameterProperties PressResult { get; set; }
        public ParameterProperties DensResult { get; set; }
        public ParameterProperties Dens15Result { get; set; }
        public ParameterProperties Dens20Result { get; set; }
        public ParameterProperties MassWaterFracResult { get; set; }
        public string PassportId { get; set; }
        public PassportChloride_Salts Chloride_Salts { get; set; }
        public ParameterProperties Impurity { get; set; }
        public ParameterProperties SulfurResult { get; set; }
        public PassportDNP DNP { get; set; }
        public ParameterProperties Yield_fraction_200 { get; set; }
        public ParameterProperties Yield_fraction_300 { get; set; }
        public ParameterProperties Yield_fraction_350 { get; set; }
        public ParameterProperties Mass_fraction_of_paraffin { get; set; }
        public ParameterProperties Mass_fraction_of_hydrogen_sulfide { get; set; }
        public ParameterProperties Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; }
        public ParameterProperties Mass_fraction_of_organic_chlorides { get; set; }
        public long Oil_Type { get; set; }
    }
    public partial class ParameterProperties
    {
        public string Desc { get; set; }
    }
    public partial class PassportChloride_Salts
    {
        public ParameterProperties Concentration { get; set; }
        public ParameterProperties MassFraction { get; set; }
    }
    public partial class PassportDNP
    {
        public ParameterProperties kPa { get; set; }
        public ParameterProperties mercury_mm { get; set; }
    }
    public partial class AdditionalData
    {
        public string DelivePoint { get; set; }
        public string AccrSertifNumber { get; set; }
        public string Laboratory { get; set; }
        public string SIKN_Number { get; set; }
        public string Laboratory_Post { get; set; }
        public string Laboratory_Factory { get; set; }
        public string Laboratory_IOF { get; set; }
        public string Delive_Post { get; set; }
        public string Delive_Factory { get; set; }
        public string Delive_IOF { get; set; }
        public string Receive_Post { get; set; }
        public string Receive_Factory { get; set; }
        public string Receive_IOF { get; set; }
        public PassportPeriodDt PassportPeriodDt { get; set; }
        public string TankerName { get; set; }
    }
    public partial class PassportPeriodDt
    {
        public long Begin { get; set; }
        public long End { get; set; }
    }
    public partial class PassportResult
    {
        public string TempResult { get; set; }
        public string PressResult { get; set; }
        public string DensResult { get; set; }
        public string Dens15Result { get; set; }
        public string Dens20Result { get; set; }
        public string MassWaterFracResult { get; set; }
        public string PassportId { get; set; }
        public PassportResultChloride_Salts Chloride_Salts { get; set; }
        public string Impurity { get; set; }
        public string SulfurResult { get; set; }
        public PassportResultDNP DNP { get; set; }
        public string Yield_fraction_200 { get; set; }
        public string Yield_fraction_300 { get; set; }
        public string Yield_fraction_350 { get; set; }
        public string Mass_fraction_of_paraffin { get; set; }
        public string Mass_fraction_of_hydrogen_sulfide { get; set; }
        public string Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; }
        public string Mass_fraction_of_organic_chlorides { get; set; }
        public string Oil_Type { get; set; }
        public string GostIndexResult { get; set; }
        public string ExtendGostIndexResult { get; set; }
    }
    public partial class PassportResultChloride_Salts
    {
        public string Concentration { get; set; }
        public string MassFraction { get; set; }
    }
    public partial class PassportResultDNP
    {
        public string kPa { get; set; }
        public string mercury_mm { get; set; }
    }
    public partial class DataARM
    {
        public string ExportPermit { get; set; } = "";
        public string Sample { get; set; } = "";
    }

    #endregion
}

namespace TN.Doc.Edit
{
    public class CfgEditPassport
    {
        public Correction Correction = new();
        public List<Parameter> Parameters = new();
        public List<Metod> Metods = new();
        public List<AdditionalInfo> AdditionalInfo = new();
    }
    public enum GUIDMetod
    {

    }
    public class Parameter
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public bool Use { get; set; }
        public bool Edit { get; set; }
    }
    public class Metod
    {
        public bool Use { get; set; }
        public int IdParameter { get; set; }
        public string Name { get; set; }
        public bool LimitValueActivate { get; set; }
        public float LimitValue { get; set; }
        public string LimitValueString { get; set; }
    }
    public class AdditionalInfo
    {
        public bool Use { get; set; }
        public string Key { set; get; }
        public string Type { set; get; }
        public string Name { set; get; }
    }
    public class Correction
    {
        public TempCorrection TempCorrection { get; set; } = new TempCorrection();
        public PressCorrection PressCorrection { get; set; } = new PressCorrection();
        public DensCorrection DensCorrection { get; set; } = new DensCorrection();
        public Dens15Correction Dens15Correction { get; set; } = new Dens15Correction();
        public Dens20Correction Dens20Correction { get; set; } = new Dens20Correction();
        public MassWaterFracCorrection MassWaterFracCorrection { get; set; } = new MassWaterFracCorrection();
        public string PassportID { get; set; } = "";
        public GOSTIndexCorrection GOSTIndexCorrection { get; set; } = new GOSTIndexCorrection();
        public Chloride_Salts Chloride_Salts { get; set; } = new Chloride_Salts();
        public Impurity Impurity { get; set; } = new Impurity();
        public SulfurCorrection SulfurCorrection { get; set; } = new SulfurCorrection();
        public DNP DNP { get; set; } = new DNP();
        public Yield_fraction_200 Yield_fraction_200 { get; set; } = new Yield_fraction_200();
        public Yield_fraction_300 Yield_fraction_300 { get; set; } = new Yield_fraction_300();
        public Yield_fraction_350 Yield_fraction_350 { get; set; } = new Yield_fraction_350();
        public Mass_fraction_of_paraffin Mass_fraction_of_paraffin { get; set; } = new Mass_fraction_of_paraffin();
        public Mass_fraction_of_hydrogen_sulfide Mass_fraction_of_hydrogen_sulfide { get; set; } = new Mass_fraction_of_hydrogen_sulfide();
        public Mass_fraction_of_methyl_and_ethyl_mercaptan Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; } = new Mass_fraction_of_methyl_and_ethyl_mercaptan();
        public Mass_fraction_of_organic_chlorides Mass_fraction_of_organic_chlorides { get; set; } = new Mass_fraction_of_organic_chlorides();
        public Data DensCorrectionValue { get; set; } = new Data();
        public ExtendGOSTIndexCorrection ExtendGOSTIndexCorrection { get; set; } = new ExtendGOSTIndexCorrection();
    }
    public class TempCorrection
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class PressCorrection
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class DensCorrection
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class Dens15Correction
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class Dens20Correction
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class MassWaterFracCorrection
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class GOSTIndexCorrection
    {
        public int IsFilled { get; set; } = 0;
        public int IsExport { get; set; } = 0;
        public Data OilClass { get; set; } = new Data();
        public Data OilType { get; set; } = new Data();
        public Data OilGroup { get; set; } = new Data();
        public Data OilSort { get; set; } = new Data();
    }
    public class Chloride_Salts
    {
        public Concentration Concentration { get; set; } = new Concentration();
        public MassFraction MassFraction { get; set; } = new MassFraction();
        public DensityOil DensityOil { get; set; } = new DensityOil();
    }
    public class Concentration
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class MassFraction
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class DensityOil
    {
        public Data Data { get; set; } = new Data();
    }
    public class Impurity
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class SulfurCorrection
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class DNP
    {
        public kPa kPa { get; set; } = new kPa();
        public mercury_mm mercury_mm { get; set; } = new mercury_mm();
    }
    public class kPa
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class mercury_mm
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class Yield_fraction_200
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class Yield_fraction_300
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class Yield_fraction_350
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class Mass_fraction_of_paraffin
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
    }
    public class Mass_fraction_of_hydrogen_sulfide
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
        public int Prop { get; set; } = 0;
    }
    public class Mass_fraction_of_methyl_and_ethyl_mercaptan
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
        public int Prop { get; set; } = 0;
    }
    public class Mass_fraction_of_organic_chlorides
    {
        public Data Data { get; set; } = new Data();
        public string Desc { get; set; } = "";
        public int Prop { get; set; } = 0;
    }
    public class Data
    {
        public float Value { get; set; } = 0;
        public int Legal { get; set; } = 0;
    }
    public class ExtendGOSTIndexCorrection
    {
        public int IsFilled { get; set; } = 0;
        public int IsExport { get; set; } = 0;
        public Data OilClass { get; set; } = new Data();
        public Data OilType { get; set; } = new Data();
        public Data OilGroup { get; set; } = new Data();
        public Data OilSort { get; set; } = new Data();
    }
    public class AdditionalData
    {
        public string DelivePoint { get; set; } = "-";
        public string AccrSertifNumber { get; set; } = "-";
        public string Laboratory { get; set; } = "-";
        public string SIKN_Number { get; set; } = "-";
        public string Laboratory_Post { get; set; } = "-";
        public string Laboratory_Factory { get; set; } = "-";
        public string Laboratory_IOF { get; set; } = "-";
        public string Delive_Post { get; set; } = "-";
        public string Delive_Factory { get; set; } = "-";
        public string Delive_IOF { get; set; } = "-";
        public string Receive_Post { get; set; } = "-";
        public string Receive_Factory { get; set; } = "-";
        public string Receive_IOF { get; set; } = "-";
        public PassportPeriodDT PassportPeriodDT { get; set; } = new PassportPeriodDT();
        public string TankerName { get; set; } = "-";
    }
    public class PassportPeriodDT
    {
        public int Begin { get; set; } = 0;
        public int End { get; set; } = 0;
    }
}