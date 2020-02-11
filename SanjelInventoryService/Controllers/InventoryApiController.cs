using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using eServiceOnline.Data;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Products;
using Sanjel.BusinessEntities.Sections.Common;
using Newtonsoft.Json;
using SanjelBusinessEntities = Sanjel.Common.BusinessEntities.Reference;
using UnitOfMeasure = Sanjel.Common.Domain.UnitOfMeasure;

namespace SanjelInventoryService.Controllers
{
    [Route("InventoryApi")]
    [ApiController]
    public class InventoryApiController : Controller
    {
        private IMemoryCache _memoryCache;

        public InventoryApiController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public ActionResult<string> Index()
        {
            //return String.Format("Choose a method to execute:\n{0}\n{1}"
            //    , " - JsonResult GetProductsCostByJobUniqueId(string uniqueId, bool byEndOfMonthWac)"
            //    , " - JsonResult GetBlendCost(string jsonBlend)"
            //    );
            return "";
        }

        [Route("GetBlendsAvailability")]
        public JsonResult GetBlendsAvailability(string jsonBlends)
        {
            InputBlends inputBlends = JsonConvert.DeserializeObject<InputBlends>(jsonBlends.ToString());
            //Collection<Blend> blends = JsonConvert.DeserializeObject<Collection<Blend>>(jsonBlends.ToString());
            Collection<OutputAvailability> outputAvailabilityCollection = new Collection<OutputAvailability>();
            string servicePointSbsId = "D675"; //HO by default

            //int bsId = 1;
            BlendFluidType bft;
            AdditiveType at;

            if (inputBlends != null && inputBlends.Blends != null)
            {
                servicePointSbsId = WebContext.DistrictSBSByName[inputBlends.District];
                foreach (Blend blend in inputBlends.Blends)
                {
                    bft = blend.Id > 0
                        ? WebContext.GetBaseBlendTypeCollection(DateTime.Today).FirstOrDefault(b => b.Id == blend.Id)
                        : WebContext.GetBaseBlendTypeCollection(DateTime.Today).FirstOrDefault(b => (b.Name ?? "").Trim().ToUpper() == blend.Name.Trim().ToUpper());

                    if (bft == null)
                        throw (new Exception("Blend Name was not found for '" + blend.Name + "'! "));

                    BlendSection bs =
                        new BlendSection()
                        {
                            Id = blend.Idx,  //bsId++,
                            BlendFluidType = new SanjelBusinessEntities.BlendFluidType() { Id = bft.Id, Name = bft.Name },
                            Quantity = blend.Quantity,
                            BlendAmountUnit = UnitOfMeasure.MeasureUnit.BlendAmountUnits.FirstOrDefault(c => c.Name == WebContext.GetUnitName(blend.Unit))
                        };

                    if (blend.Additives != null && blend.Additives.Count > 0)
                    {
                        int i = 1;
                        foreach (Additive add in (blend.Additives))
                        {
                            at = add.Id > 0
                                ? WebContext.GetAdditiveTypeCollection(DateTime.Today).FirstOrDefault(a => a.Id == add.Id)
                                : WebContext.GetAdditiveTypeCollection(DateTime.Today).FirstOrDefault(a => (a.Name ?? "").Trim().ToUpper() == add.Name.Trim().ToUpper());

                            if (at == null)
                                throw (new Exception("Additive Name was not found for '" + add.Name + "'! "));

                            if (bs.BlendAdditiveSections == null)
                                bs.BlendAdditiveSections = new Collection<BlendAdditiveSection>();

                            BlendAdditiveSection bas =
                                new BlendAdditiveSection()
                                {
                                    Id = i++,
                                    AdditiveType = new SanjelBusinessEntities.AdditiveType() { Id = at.Id, Name = at.Name },
                                    AdditionMethod = new SanjelBusinessEntities.AdditiveMethodType(),
                                    AdditiveAmountUnit = UnitOfMeasure.MeasureUnit.BlendAdditiveUnits.FirstOrDefault(c => c.Name == WebContext.GetUnitName(add.Unit)),
                                    Amount = add.Quantity,
                                    BaseName = ""
                                };
                            bs.BlendAdditiveSections.Add(bas);
                        }
                    }
                    ProcessBlendSectionAvailability(bs, servicePointSbsId, ref outputAvailabilityCollection, DateTime.Today);
                }
            }
            return this.Json(new { costCollection = outputAvailabilityCollection }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        private void ProcessBlendSectionAvailability(BlendSection bs, string servicePointSbsId, ref Collection<OutputAvailability> outputAvailabilityCollection, DateTime chemicalsAsOfDate)
        {
            WebContext.PrepareBlendSection(ref bs);
            BlendChemical blendChemical = WebContext.ConvertToBlendChemical(bs, chemicalsAsOfDate);

            if (blendChemical != null)
            {
                try
                {
                    Collection<BlendChemicalSection> allBlendBreakDowns = WebContext.GetAllBreakDowns(bs, blendChemical);
                    //string warehouse = @"'%20'";
                    string warehouse = servicePointSbsId.Substring(1, 3) + "20" ;
                    string iinList = "";

                    foreach (BlendChemicalSection bcs in allBlendBreakDowns)
                    {
                        if (!String.IsNullOrEmpty(bcs.BlendChemical.Product.InventoryNumber))
                        {
                            if (iinList.Length == 0)
                                iinList += "(";

                            iinList += (iinList.Length == 1 ? "" : ",") + "'" + bcs.BlendChemical.Product.InventoryNumber + "'";
                        }
                    }

                    if (iinList.Length > 0) iinList += ")";

                    Collection<Chemical> avail = WebContext.GetInventoryAvailability(warehouse, iinList);

                    foreach (BlendChemicalSection bcs in allBlendBreakDowns)
                    {
                        Chemical cm = avail.FirstOrDefault(c => c.IIN == (bcs.BlendChemical.Product.InventoryNumber ?? "-"));

                        outputAvailabilityCollection.Add(
                            new OutputAvailability()
                            {
                                Id = bs.Id,  //bcs.Id,
                                Name = cm != null ? cm.Name : "NA", //bcs.Name,
                                IIN = bcs.BlendChemical.Product.InventoryNumber ?? "",
                                Quantity = bcs.Amount,
                                Unit = bcs.Unit.Name,
                                InventoryQuantity = cm != null ? cm.Quantity ?? 0 : -1,
                                InventoryUnit = cm != null ? cm.Unit : "NA"
                            });
                    }
                }
                catch (Exception ex)
                {
                    var code = ex.Message.ToString();
                    code = code.Length <= 49 ? code : code.Substring(0, 49);
                    outputAvailabilityCollection.Add(
                        new OutputAvailability() {
                            Id = bs.Id,
                            Name = bs.BlendFluidType.Name,
                            IIN = "",
                            Quantity = bs.Quantity ?? 0,
                            Unit = bs.BlendAmountUnit.Name,
                            InventoryQuantity = -1,
                            InventoryUnit = "NA"
                        });
                }
            }
            else
            {
                outputAvailabilityCollection.Add(
                    new OutputAvailability() {
                        Id = bs.Id,
                        Name = bs.BlendFluidType.Name,
                        IIN = "",
                        Quantity = bs.Quantity ?? 0,
                        Unit = bs.BlendAmountUnit.Name,
                        InventoryQuantity = -1,
                        InventoryUnit = "NA"
                    });
            }
        }

    }
}
