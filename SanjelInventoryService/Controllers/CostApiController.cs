﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using eServiceOnline.Gateway;
using eServiceOnline.Data;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Products;
using Sanjel.Common.BusinessEntities.Mdd.Products;
using Sanjel.BusinessEntities.Jobs;
using Sanjel.BusinessEntities.Sections.Common;
using Sanjel.BusinessEntities.ServiceReports;
using Newtonsoft.Json;
using SanjelBusinessEntities = Sanjel.Common.BusinessEntities.Reference;
using UnitOfMeasure = Sanjel.Common.Domain.UnitOfMeasure;
using eServiceOnline.Data;

namespace SanjelInventoryService.Controllers
{
    [Route("CostApi")]
    [ApiController]
    public class CostApiController : Controller
    {
        private IMemoryCache _memoryCache;

        public CostApiController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public ActionResult<string> Index()
        {
            return String.Format("Choose a method to execute:\n{0}\n{1}\n{2}"
                , " - /CostApi/GetProductsCostByJobUniqueId(string uniqueId, bool byEndOfMonthWac)"
                , " - /CostApi/GetBlendsCost(string jsonBlends)"
                , " - /InventoryApi/GetBlendsAvailability(string jsonBlends)"
                );
        }

        [Route("GetBlendsCost")]
        public JsonResult GetBlendsCost(string jsonBlends)
        {
            InputBlends inputBlends = JsonConvert.DeserializeObject<InputBlends>(jsonBlends.ToString());
            Collection<OutputCost> outputCostCollection = new Collection<OutputCost>();
            var freightCost = 0.00;
            string servicePointSbsId = "D675"; //HO by default

            int bsId = 1;
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

                    ProcessBlendSectionCost(bs, servicePointSbsId, freightCost, ref outputCostCollection, DateTime.Today, DateTime.Today, inputBlends.WithDetails);
                }
            }

            return this.Json(new { costCollection = outputCostCollection }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        [Route("GetProductsCostByJobUniqueId")] 
        public JsonResult GetProductsCostByJobUniqueId(string uniqueId, bool byEndOfMonthWac)
        {
            Collection<OutputCost> outputCostCollection = new Collection<OutputCost>();
            DateTime jobDate = DateTime.MinValue;
            int jobNumber = 0;
            string servicePointSbsId = "";
            var freightCost = 0.00;

            Job job = DataGateway.GetJobByUniqueId(uniqueId);
            if (job != null)
            {
                jobDate = job.JobDateTime;
                jobNumber = job.JobNumber;
                servicePointSbsId = WebContext.DistrictSBS[(job.JobData != null && job.JobData.ServicePoint != null) ? job.JobData.ServicePoint.Id : -1] ?? "";
            }

            if (jobDate != DateTime.MinValue && servicePointSbsId != "")
            {
                DateTime costAsOfDate = jobDate.Date.AddMonths(byEndOfMonthWac ? 1 : 0);
                DateTime chemAsOfDate = DateTime.Today;
                PumpingServiceReport sr = (PumpingServiceReport)DataGateway.GetServiceReportByUniqueId(uniqueId);

                if (
                    sr != null 
                    && sr.PumpingSection != null 
                    && sr.PumpingSection.ProductSection != null 
                    && sr.PumpingSection.ProductSection.BlendSections != null 
                    && sr.PumpingSection.ProductSection.BlendSections.Any()
                    && WebContext.GetBlendChemicalCollection(chemAsOfDate) != null 
                    && WebContext.GetPurchasePriceCollection(costAsOfDate) != null
                    )
                {
                    foreach (BlendSection bs in sr.PumpingSection.ProductSection.BlendSections)
                    {
                        ProcessBlendSectionCost(bs, servicePointSbsId, freightCost, ref outputCostCollection, costAsOfDate, chemAsOfDate, false);
                    }
                }
            }
            //return this.Json(new { jobUniqueId = uniqueId, JobNumber = jobNumber, JobDate = jobDate, costCollection = outputCostCollection }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            return this.Json(new { jobUniqueId = uniqueId, JobNumber = jobNumber, JobDate = jobDate, costCollection = outputCostCollection });
        }

        private void ProcessBlendSectionCost(BlendSection bs, string servicePointSbsId, double freightCost, ref Collection<OutputCost> outputCostCollection, DateTime costAsOfDate, DateTime chemicalsAsOfDate, bool includeDetails)
        {
            WebContext.PrepareBlendSection(ref bs);
            BlendChemical blendChemical = WebContext.ConvertToBlendChemical(bs, chemicalsAsOfDate);

            if (blendChemical != null)
            {
                try
                {
                    var totalCost = 0.0;
                    Collection<BlendChemicalSection> allBlendBreakDowns = WebContext.GetAllBreakDowns(bs, blendChemical);

                    foreach (BlendChemicalSection bcs in allBlendBreakDowns)
                    {
                        var cost = BlendCostItem.CalculateCost(WebContext.GetPurchasePriceCollection(costAsOfDate), bcs.BlendChemical, freightCost, true, servicePointSbsId);
                        totalCost += cost * bcs.Amount;

                        if (includeDetails)
                            outputCostCollection.Add(
                                new OutputCost() {
                                    Id = bs.Id,
                                    Cost = cost * bcs.Amount,
                                    Name = bcs.BlendChemical.Name,
                                    IIN = bcs.BlendChemical.Product.InventoryNumber ?? "",
                                    PbCode = bcs.BlendChemical.Product.PriceCode.ToString() ?? "",
                                    Quantity = bcs.Amount,
                                    Unit = bcs.Unit.Name,
                                    IsDetail = true
                                });
                    }

                    var blend = string.IsNullOrEmpty(blendChemical.Description) ? blendChemical.Name : blendChemical.Description;
                    var inn = blendChemical.Product.InventoryNumber ?? "";
                    var code = blendChemical.Product.PriceCode == 0 ? "" : blendChemical.Product.PriceCode.ToString();

                    outputCostCollection.Add(
                        new OutputCost() {
                            Id = bs.Id,
                            Cost = totalCost,
                            Name = blend,
                            IIN = inn,
                            PbCode = code,
                            Quantity = bs.Quantity ?? 0,
                            Unit = bs.BlendAmountUnit == null ? "" : bs.BlendAmountUnit.Name,
                            IsDetail = false
                        });
                }
                catch (Exception ex)
                {
                    var code = ex.Message.ToString();
                    code = code.Length <= 49 ? code : code.Substring(0, 49);
                    outputCostCollection.Add(
                        new OutputCost() {
                            Id = bs.Id,
                            Cost = 0,
                            Name = bs.BlendFluidType.Name,
                            IIN = "",
                            PbCode = code,
                            Quantity = bs.Quantity ?? 0,
                            Unit = bs.BlendAmountUnit == null ? "" : bs.BlendAmountUnit.Name,
                            IsDetail = false
                        });
                }
            }
            else
            {
                outputCostCollection.Add(
                    new OutputCost() {
                        Id = bs.Id,
                        Cost = 0,
                        Name = bs.BlendFluidType.Name,
                        IIN = "",
                        PbCode = "NoChemical",
                        Quantity = bs.Quantity ?? 0,
                        Unit = bs.BlendAmountUnit == null ? "" : bs.BlendAmountUnit.Name,
                        IsDetail = false
                    });
            }
        }
    }
}