using Sesi.SanjelData.Entities.Common.BusinessEntities.Products;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Inventory;
using eServiceOnline.Gateway;
using Sanjel.BusinessEntities.Sections.Common;
using Sanjel.Common.BusinessEntities.Mdd.Products;

namespace eServiceOnline.Data
{
    public class WebContext
    {
        private WebContext()
        {
        }

        private static Collection<BlendChemical> blendChemicalCollection;
        private static Collection<BlendRecipe> blendRecipeCollection;
        private static Collection<BlendChemicalSection> blendChemicalSectionCollection;
        private static Collection<Product> productCollection;

        private static Collection<BlendFluidType> baseBlendTypeCollection;
        private static Collection<AdditiveType> additiveTypeCollection;

        private static Collection<PurchasePrice> purchasePriceCollection;

        private static DateTime LastBaseBlendAsOfDate = DateTime.MinValue;
        private static DateTime LastAdditiveAsOfDate = DateTime.MinValue;
        private static DateTime LastCostAsOfDate = DateTime.MinValue;
        private static DateTime LastChemicalAsOfDate = DateTime.MinValue;
        private static DateTime LastRunDateTime = DateTime.MinValue;

        static int dataEnforcedRefreshIntervalForInactivityMinutes = 10;

        private static WebContext instance;

        public static WebContext Instance
        {
            get
            {
                if (instance == null)
                    instance = new WebContext();

                return instance;
            }
        }

        public static Collection<BlendChemical> GetBlendChemicalCollection(DateTime chemicalsAsOfDate)
        {
            return SetUpDataSourceCollections(chemicalsAsOfDate) ? blendChemicalCollection : null;
        }

        public static Collection<BlendRecipe> GetBlendRecipeCollection(DateTime chemicalsAsOfDate)
        {
            return SetUpDataSourceCollections(chemicalsAsOfDate) ? blendRecipeCollection : null;
        }

        public static Collection<BlendChemicalSection> GetBlendChemicalSectionCollection(DateTime chemicalsAsOfDate)
        {
            return SetUpDataSourceCollections(chemicalsAsOfDate) ? blendChemicalSectionCollection : null;
        }

        public static Collection<Product> GetProductCollection(DateTime chemicalsAsOfDate)
        {
            return SetUpDataSourceCollections(chemicalsAsOfDate) ? productCollection : null;
        }

        public static Collection<BlendFluidType> GetBaseBlendTypeCollection(DateTime asOfDate)
        {
            if (baseBlendTypeCollection == null || LastBaseBlendAsOfDate != asOfDate || (DateTime.Now - LastRunDateTime).TotalMinutes > dataEnforcedRefreshIntervalForInactivityMinutes)
            {
                baseBlendTypeCollection = DataGateway.GetBaseBlendTypesAsOfDate(asOfDate);
                LastBaseBlendAsOfDate = asOfDate;
            }

            return baseBlendTypeCollection;
        }

        public static Collection<AdditiveType> GetAdditiveTypeCollection(DateTime asOfDate)
        {
            if (additiveTypeCollection == null || LastAdditiveAsOfDate != asOfDate || (DateTime.Now - LastRunDateTime).TotalMinutes > dataEnforcedRefreshIntervalForInactivityMinutes)
            {
                additiveTypeCollection = DataGateway.GetAdditiveTypesAsOfDate(asOfDate);
                LastAdditiveAsOfDate = asOfDate;
            }

            return additiveTypeCollection;
        }

        public static Collection<PurchasePrice> GetPurchasePriceCollection(DateTime costAsOfDate)
        {
            if (purchasePriceCollection == null || LastCostAsOfDate != costAsOfDate || (DateTime.Now - LastRunDateTime).TotalMinutes > dataEnforcedRefreshIntervalForInactivityMinutes)
            {
                purchasePriceCollection = DataGateway.GetPurchasePricesAsOfDate(costAsOfDate);
                LastCostAsOfDate = costAsOfDate;
            }

            return purchasePriceCollection;
        }

        static public void PrepareBlendSection(ref BlendSection bs)
        {
            //Replace "LITEmix PRO RD" (294) with "LITEmix PRO" (289) for cost calculation as per Jason's sudgestion
            //Commented on Dec 02, 2020 to check if an original issue was fixed (nobody remember now what it was)
            //if (bs.BlendFluidType != null && bs.BlendFluidType.Id == 294)
            //    bs.BlendFluidType.Id = 289;

            //Remove empty records in additives list
            if (bs.BlendAdditiveSections != null && bs.BlendAdditiveSections.Count > 0)
            {
                for (int i = bs.BlendAdditiveSections.Count - 1; i >= 0; i--)
                {
                    if (bs.BlendAdditiveSections[i] == null || bs.BlendAdditiveSections[i].AdditiveType == null || bs.BlendAdditiveSections[i].AdditiveType.Name == "" || bs.BlendAdditiveSections[i].AdditiveType.Id == 0)
                        bs.BlendAdditiveSections.RemoveAt(i);
                }
            }
        }

        static public BlendChemical ConvertToBlendChemical(BlendSection bs, DateTime chemicalsAsOfDate)
        {
            BlendChemical blendChemical = null;
            Collection<BlendChemical> blendChemicalList = MetaShare.Common.Core.Entities.Common.DeepClone(GetBlendChemicalCollection(chemicalsAsOfDate)) as Collection<BlendChemical>;
            Collection<AdditionMethod> additionMethodList = MetaShare.Common.Core.Entities.Common.DeepClone(DataGateway.GetAdditionMethodAsOfDate(chemicalsAsOfDate)) as Collection<AdditionMethod>;
            Collection<AdditiveBlendMethod> additiveBlendMethodList = MetaShare.Common.Core.Entities.Common.DeepClone(DataGateway.GetAdditiveBlendMethodAsOfDate(chemicalsAsOfDate)) as Collection<AdditiveBlendMethod>;
            Collection<BlendAdditiveMeasureUnit> blendAdditiveMeasureUnitList = MetaShare.Common.Core.Entities.Common.DeepClone(DataGateway.GetBlendAdditiveMeasureUnitAsOfDate(chemicalsAsOfDate)) as Collection<BlendAdditiveMeasureUnit>;
            try
            {
                blendChemical = BlendSection.CovertToBlendChemicalFromBlendSection(blendChemicalList, bs, additionMethodList, additiveBlendMethodList, blendAdditiveMeasureUnitList);
            }
            catch (Exception ex)
            {
            }
            return blendChemical;
        }

        static public Collection<BlendChemicalSection> GetAllBreakDowns(BlendSection bs, BlendChemical blendChemical)
        {
            //var blendQuantity = (bs.Quantity ?? 0.0) * (bs.BlendAmountUnit.Abbreviation.Equals("m3") || bs.BlendAmountUnit.Abbreviation.Equals("t") ? 1000 : 1);
            var blendQuantity = (bs.Quantity ?? 0.0) * 1;

            Collection<BlendAdditiveMeasureUnit> blendAdditiveMeasureUnitList = DataGateway.GetBlendAdditiveMeasureUnitAsOfDate(DateTime.Now);
            BlendAdditiveMeasureUnit unit = blendAdditiveMeasureUnitList.FirstOrDefault(p => p.Name == bs.BlendAmountUnit.Name);

            BlendChemicalSection baseBlendSection = blendChemical.BlendRecipe.BlendChemicalSections.FirstOrDefault(p => p.IsBaseBlend);
            List<BlendChemicalSection> additiveBlendSections = blendChemical.BlendRecipe.BlendChemicalSections.Where(p => !p.IsBaseBlend).ToList();
            BlendChemical baseBlend = baseBlendSection?.BlendChemical;

            Collection<BlendChemicalSection> allBlendBreakDowns;
            Collection<BlendChemicalSection> baseBlendBreakDowns;
            Collection<BlendChemicalSection> additiveBlendBreakDowns;
            Collection<BlendChemicalSection> additionalBlendBreakDowns;
            double totalBlendWeight;
            double baseBlendWeight;
            //BlendBreakDownCalculator.GetAllBlendBreakDown(blendChemical.BlendRecipe, blendQuantity, false, bs.MixWaterRequirement ?? 1.0, out allBlendBreakDowns, out baseBlendBreakDowns, out additiveBlendBreakDowns, out additionalBlendBreakDowns, out totalBlendWeight, out baseBlendWeight);
            BlendBreakDownCalculator.GetAllBlendBreakDown1(blendChemical.BlendRecipe, blendQuantity, null, false, bs.MixWaterRequirement ?? 1.0, out allBlendBreakDowns, out baseBlendBreakDowns, out additiveBlendBreakDowns, out additionalBlendBreakDowns, out totalBlendWeight, out baseBlendWeight, unit);

            return allBlendBreakDowns;
        }

        public static Collection<Chemical> GetInventoryAvailability(string warehouse, string iinList)
        {
            Collection<Chemical> avail = new Collection<Chemical>();

            SqlConnection sqlConn = null;
            SqlCommand cmd = null;
            SqlDataAdapter da = null;
            DataTable dt = null;

            string cmdStr = String.Format(
                    "SELECT [Warehouse],[Location],[Item Number],[Product Name],[Physical Inventory],[Unit] FROM SBS_VIEWS.dbo.InventorySummaryView where Warehouse like {0} and [Item Number] in {1}"
                    , warehouse, iinList);

            try
            {
                sqlConn = new SqlConnection(@"Data Source=Sanjel27\DW;Initial Catalog=SBS_VIEWS;User ID=sa;Password=pass@word1;");
                cmd = new SqlCommand(cmdStr, sqlConn);
                cmd.CommandType = CommandType.Text;
                da = new SqlDataAdapter(cmd);
                dt = new DataTable();
                sqlConn.Open();

                da.Fill(dt);
            }
            catch (Exception ex)
            {
                if (sqlConn != null && sqlConn.State != ConnectionState.Closed)
                    sqlConn.Close();
                sqlConn.Dispose();
                dt.Dispose();
                da.Dispose();
                cmd.Dispose();

                throw ex;
            }

            if (sqlConn.State != ConnectionState.Closed)
                sqlConn.Close();

            sqlConn.Dispose();

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    avail.Add(
                        new Chemical()
                        {
                            Id = 0,
                            Name = dr["Product Name"].ToString(),
                            IIN = dr["Item Number"].ToString(),
                            Quantity = double.Parse(dr["Physical Inventory"].ToString()),
                            Unit = dr["Unit"].ToString()
                        });
                }
            }
            dt.Dispose();
            da.Dispose();
            cmd.Dispose();

            return avail;
        }

        private static bool SetUpDataSourceCollections(DateTime chemicalsAsOfDate)
        {
            //bool result = true;
            bool chemicalUpdated = false;
            double inactivityMinutes = (DateTime.Now - LastRunDateTime).TotalMinutes;
            LastRunDateTime = DateTime.Now;

            if (
               blendChemicalCollection == null 
               || blendRecipeCollection == null 
               || blendChemicalSectionCollection == null 
               || productCollection == null 
               || LastChemicalAsOfDate != chemicalsAsOfDate 
               || inactivityMinutes > dataEnforcedRefreshIntervalForInactivityMinutes
               )
            {
                blendChemicalCollection = DataGateway.GetBlendChemicalsAsOfDate(chemicalsAsOfDate);
                blendRecipeCollection = DataGateway.GetBlendRecipesAsOfDate(chemicalsAsOfDate);
                blendChemicalSectionCollection = DataGateway.GetBlendChemicalSectionsAsOfDate(chemicalsAsOfDate);
                productCollection = DataGateway.GetProductsAsOfDate(chemicalsAsOfDate);

                chemicalUpdated = true;
                LastChemicalAsOfDate = chemicalsAsOfDate;
            }

            if (
                chemicalUpdated 
                && blendChemicalCollection != null && blendChemicalCollection.Any() 
                && blendRecipeCollection != null && blendRecipeCollection.Any() 
                && blendChemicalSectionCollection != null && blendChemicalSectionCollection.Any() 
                && productCollection != null && productCollection.Any()
                )
            {
                foreach (BlendRecipe br in blendRecipeCollection)
                {
                    if (br.BlendChemicalSections == null)
                        br.BlendChemicalSections = new List<BlendChemicalSection>();

                    foreach (BlendChemicalSection bhs in blendChemicalSectionCollection)
                    {
                        if (bhs.OwnerId == br.Id)
                            br.BlendChemicalSections.Add(bhs);
                    }
                }

                foreach (BlendChemical bh in blendChemicalCollection)
                {
                    bool found = false;

                    foreach (BlendRecipe br in blendRecipeCollection)
                    {
                        if (bh.BlendRecipe.Id > 0 && bh.BlendRecipe.Id == br.Id)
                        {
                            bh.BlendRecipe = br;
                            found = true;
                            break;
                        }
                    }

                    if (!found && bh.BlendRecipe.Id == 0)
                        bh.BlendRecipe.BlendChemicalSections = new List<BlendChemicalSection>();

                    found = false;

                    foreach (Product pr in productCollection)
                    {
                        if (bh.Product.Id > 0 && bh.Product.Id == pr.Id)
                        {
                            bh.Product = pr;
                            found = true;
                            break;
                        }
                    }

                    if (!found && bh.Product.Id == 0)
                        bh.Product = new Product();
                }

                foreach (BlendChemicalSection bhs in blendChemicalSectionCollection)
                {
                    bool found = false;

                    foreach (BlendRecipe br in blendRecipeCollection)
                    {
                        if (bhs.BlendRecipe.Id == br.Id)
                        {
                            bhs.BlendRecipe = br;
                            found = true;
                            break;
                        }
                    }

                    if (!found && bhs.BlendRecipe.Id == 0)
                        bhs.BlendRecipe.BlendChemicalSections = new List<BlendChemicalSection>();

                    found = false;

                    foreach (BlendChemical bh in blendChemicalCollection)
                    {
                        if (bhs.BlendChemical.Id == bh.Id)
                        {
                            bhs.BlendChemical = bh;
                            found = true;
                            break;
                        }
                    }
                    if (!found && bhs.BlendChemical.Id == 0)
                    {
                        bhs.BlendChemical.BlendRecipe = new BlendRecipe();
                        bhs.BlendChemical.BlendRecipe.BlendChemicalSections = new List<BlendChemicalSection>();
                    }
                }
            }

            return
                blendChemicalCollection != null && blendChemicalCollection.Any()
                && blendRecipeCollection != null && blendRecipeCollection.Any()
                && blendChemicalSectionCollection != null && blendChemicalSectionCollection.Any()
                && productCollection != null && productCollection.Any()
                ;

        }


        static public Dictionary<int, string> DistrictSBS = new Dictionary<int, string>()
        {
            { 61, "D607" },
            { 62, "D675" },
            { 65, "D606" },
            { 66, "D615" },
            { 67, "D602" },
            { 69, "D600" },
            { 70, "D617" },
            { 71, "D604" },
            { 72, "D616" },
            { 78, "D651" },
            { 81, "D603" },
            { 85, "D612" },
            { 87, "D653" },
            { 88, "D618" },
        };

        static public Dictionary<string, string> DistrictSBSByName = new Dictionary<string, string>()
        {
            { "Lloydminster", "D607" },
            { "Calgary - Head Office", "D675" },
            { "Lac La Biche", "D606" },
            { "Fort St John", "D615" },
            { "Fort St. John", "D615" },
            { "Edmonton", "D602" },
            { "Brooks", "D600" },
            { "Swift Current", "D617" },
            { "Grande Prairie", "D604" },
            { "Estevan", "D616" },
            { "Calgary - Maintenance", "D651" },
            { "Edson", "D603" },
            { "Red Deer", "D612" },
            { "Calgary - Lab", "D653" },
            { "Kindersley", "D618" },
        };

        public static string GetUnitName(string unit)
        {
            string result;

            switch (unit.ToUpper())
            {
                case "SCM":
                    result = "SCM";
                    break;
                case "T":
                case "TONNES":
                    result = "Tonnes";
                    break;
                case "KG":
                case "KGS":
                case "KILOGRAMS":
                    result = "Kilograms";
                    break;
                case "L":
                case "LITRES":
                    result = "Litres";
                    break;
                case "M3":
                case "CUBIC METERS":
                    result = "Cubic Meters";
                    break;
                case "%":
                case "PERCENT":
                    result = "Percent";
                    break;
                case "KG/M3":
                    result = "Kg/m3";
                    break;
                case "L/M3":
                    result = "l/m3";
                    break;
                case "% BWOW":
                    result = "% BWOW";
                    break;
                default:
                    result = unit;
                    break;
            };

            return result;
        }

    }

    public class InputBlends
    {
        public string District { get; set; }
        public bool WithDetails { get; set; }
        public Collection<Blend> Blends { get; set; }
    }

    public class OutputCost : Chemical
    {
        //public int BlendSectionId { get; set; }
        //public string BlendName { get; set; }
        //public string INN { get; set; }
        //public string Code { get; set; }
        //public double CostAmount { get; set; }
        //public int ParentBlendSectionId { get; set; }
        //public double Quantity { get; set; }
        //public string UnitOfMeasure { get; set; }
        public string PbCode { get; set; }
        public bool IsDetail { get; set; }
    }


    public class OutputAvailability : Chemical
    {
        //public int ParentBlendSectionId { get; set; }
        //public int BlendSectionId { get; set; }
        //public string BlendName { get; set; }
        //public string INN { get; set; }
        //public string Code { get; set; }
        //public double CostAmount { get; set; }
        //public double Quantity { get; set; }
        //public string UnitOfMeasure { get; set; }
        public double InventoryQuantity { get; set; }
        public string InventoryUnit { get; set; }
    }

    public class Blend : Chemical
    {
        public int Idx { get; set; }
        public string WaterMix { get; set; }
        public List<Additive> Additives { get; set; }
    }

    public class Additive : Chemical
    {
        //public double Amount { get; set; }
        //public string AmountUnit { get; set; }
    }

    public class Chemical
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IIN { get; set; }
        public double? Quantity { get; set; }
        public string Unit { get; set; }
        public double Cost { get; set; }
    }

}

