/*
 * using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sanjel.BusinessEntities.ServiceReports;
using Sanjel.BusinessEntities.Jobs;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Organization;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Products;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Inventory;

namespace eServiceOnline.Data
{
    public interface IeServiceWebContext
    {
        Collection<PurchasePrice> GetPurchasePricesAsOfDate(DateTime effectiveDateTime);
        Collection<BlendRecipe> GetBlendRecipesAsOfDate(DateTime effectiveDateTime);
        Collection<BlendChemical> GetBlendChemicalsAsOfDate(DateTime effectiveDateTime);
        Collection<BlendChemicalSection> GetBlendChemicalSectionsAsOfDate(DateTime effectiveDateTime);
        Collection<Product> GetProductsAsOfDate(DateTime effectiveDateTime);
        Job GetJobByUniqueId(string jobUniqueId);
        ServiceReport GetServiceReportByUniqueId(string uniqueId);
        List<ServicePoint> GetServicePoints();
    }
}
*/