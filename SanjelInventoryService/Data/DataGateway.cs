﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sanjel.BusinessEntities.ServiceReports;
using Sanjel.BusinessEntities.Jobs;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Organization;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Products;
using Sesi.SanjelData.Entities.Common.BusinessEntities.Inventory;
using Sesi.SanjelData.Services.Interfaces.Common.BusinessEntities.Organization;
using Sesi.SanjelData.Services.Interfaces.Common.BusinessEntities.Products;
using Sesi.SanjelData.Services.Interfaces.Common.BusinessEntities.Inventory;

namespace eServiceOnline.Gateway
{
    public class DataGateway //: IeServiceGateway
    {
        //private static eServiceGateway instance = new eServiceGateway();

        //public static eServiceGateway Instance
        //{
        //    get { return instance; }
        //}

        public static Collection<PurchasePrice> GetPurchasePricesAsOfDate(DateTime effectiveDateTime)
        {
            IPurchasePriceService purchasePriceService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IPurchasePriceService>();
            return new Collection<PurchasePrice>(purchasePriceService.SelectAllByDateTime(effectiveDateTime));
        }

        public static Collection<BlendRecipe> GetBlendRecipesAsOfDate(DateTime effectiveDateTime)
        {
            IBlendRecipeService blendRecipeService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IBlendRecipeService>();
            //return new Collection<BlendRecipe>(blendRecipeService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<BlendRecipe>(blendRecipeService.SelectAll());
        }

        public static Collection<BlendChemical> GetBlendChemicalsAsOfDate(DateTime effectiveDateTime)
        {
            IBlendChemicalService blendChemicalService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IBlendChemicalService>();
            //return new Collection<BlendChemical>(blendChemicalService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<BlendChemical>(blendChemicalService.SelectAll());
        }

        public static Collection<AdditiveType> GetAdditiveTypesAsOfDate(DateTime effectiveDateTime)
        {
            IAdditiveTypeService additiveTypeService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IAdditiveTypeService>();
            //return new Collection<AdditiveType>(additiveTypeService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<AdditiveType>(additiveTypeService.SelectAll());
        }
        public static Collection<BlendFluidType> GetBaseBlendTypesAsOfDate(DateTime effectiveDateTime)
        {
            IBlendFluidTypeService blendFluidTypeService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IBlendFluidTypeService>();
            //return new Collection<BlendFluidType>(blendFluidTypeService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<BlendFluidType>(blendFluidTypeService.SelectAll());
        }

        public static Collection<BlendChemicalSection> GetBlendChemicalSectionsAsOfDate(DateTime effectiveDateTime)
        {
            IBlendChemicalSectionService blendChemicalSectionService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IBlendChemicalSectionService>();
            //return new Collection<BlendChemicalSection>(blendChemicalSectionService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<BlendChemicalSection>(blendChemicalSectionService.SelectAll());
        }

        public static Collection<Product> GetProductsAsOfDate(DateTime effectiveDateTime)
        {
            IProductService productService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IProductService>();
            //return new Collection<Product>(productService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<Product>(productService.SelectAll());
        }

        public static Collection<AdditionMethod> GetAdditionMethodAsOfDate(DateTime effectiveDateTime)
        {
            IAdditionMethodService additionMethodService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IAdditionMethodService>();
            //return new Collection<AdditionMethod>(additionMethodService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<AdditionMethod>(additionMethodService.SelectAll());
        }

        public static Collection<AdditiveBlendMethod> GetAdditiveBlendMethodAsOfDate(DateTime effectiveDateTime)
        {
            IAdditiveBlendMethodService additiveBlendMethodService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IAdditiveBlendMethodService>();
            //return new Collection<AdditiveBlendMethod>(additiveBlendMethodService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<AdditiveBlendMethod>(additiveBlendMethodService.SelectAll());
        }

        public static Collection<BlendAdditiveMeasureUnit> GetBlendAdditiveMeasureUnitAsOfDate(DateTime effectiveDateTime)
        {
            IBlendAdditiveMeasureUnitService blendAdditiveMeasureUnitService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IBlendAdditiveMeasureUnitService>();
            //return new Collection<BlendAdditiveMeasureUnit>(blendAdditiveMeasureUnitService.SelectAllByDateTime(effectiveDateTime));
            return new Collection<BlendAdditiveMeasureUnit>(blendAdditiveMeasureUnitService.SelectAll());
        }

        public static Job GetJobByUniqueId(string jobUniqueId)
        {
            if (string.IsNullOrEmpty(jobUniqueId)) return null;

            Sanjel.Services.Interfaces.IJobService jobService = MetaShare.Common.ServiceModel.Services.ServiceFactory.Instance.GetService(typeof(Sanjel.Services.Interfaces.IJobService)) as Sanjel.Services.Interfaces.IJobService;
            if (jobService == null) throw new Exception("jobService must be registered in service factory");

            return jobService?.GetJobByUniqueId(jobUniqueId);
        }

        public static Sanjel.BusinessEntities.Programs.Program GetProgramById(int programId)
        {
            if (programId <= 0) return null;

            Sanjel.Services.Interfaces.IProgramService programService = MetaShare.Common.ServiceModel.Services.ServiceFactory.Instance.GetService(typeof(Sanjel.Services.Interfaces.IProgramService)) as Sanjel.Services.Interfaces.IProgramService;
            if (programService == null) throw new Exception("programService must be registered in service factory");

            return programService?.GetProgramById(programId);
        }

        public static ServiceReport GetServiceReportByUniqueId(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return null;

            Sanjel.Services.Interfaces.IServiceReportService serviceReportrService = MetaShare.Common.ServiceModel.Services.ServiceFactory.Instance.GetService(typeof(Sanjel.Services.Interfaces.IServiceReportService)) as Sanjel.Services.Interfaces.IServiceReportService;
            if (serviceReportrService == null) throw new Exception("serviceReportrService must be registered in service factory");

            return serviceReportrService?.GetServiceReportByUniqueId(uniqueId);
        }

        public static List<ServicePoint> GetServicePoints()
        {
            IServicePointService servicePointService = MetaShare.Common.Core.CommonService.ServiceFactory.Instance.GetService<IServicePointService>();
            if (servicePointService == null) throw new Exception("servicePointService must be registered in service factory");

            return servicePointService.SelectAll();
        }



        //public ServiceReport GetServiceReportByUniqueId(string serviceReportUniqueId)
        //{
        //    IServiceReportService service = ProxyFactory.GetProxy<IServiceReportService>();
        //    {
        //        try
        //        {
        //            ServiceReport serviceReport = service.GetServiceReportByUniqueId(serviceReportUniqueId);
        //            //ContextedGateway.CacheToLocal(serviceReport);
        //            return serviceReport;
        //        }
        //        catch (Exception ex)
        //        {
        //            WCFProxyBuilder.AbortCommunicationObject(service);
        //            //throw new OfflineException(ex);
        //        }
        //        finally
        //        {
        //            WCFProxyBuilder.DisposeCommunicationObject(service);
        //        }
        //    }
        //    return null;
        //}

    }
}
