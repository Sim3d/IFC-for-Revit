﻿//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a Revit element as IfcBuildingElementProxy.
   /// </summary>
   class ProxyElementExporter
   {
      /// <summary>
      /// Exports an element as building element proxy.
      /// </summary>
      /// <remarks>
      /// This function is called from the Export function, but can also be called directly if you do not
      /// want CreateInternalPropertySets to be called.
      /// </remarks>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>The handle if created, null otherwise.</returns>
      public static IFCAnyHandle ExportBuildingElementProxy(ExporterIFC exporterIFC, Element element,
          GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         if (element == null || geometryElement == null)
            return null;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcBuildingElementProxy;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return null;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle buildingElementProxy = null;
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, element))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);

                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, element,
                      categoryId, geometryElement, bodyExporterOptions, null, ecData, true);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return null;
                  }

                  string guid = GUIDUtil.CreateGUID(element);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                  buildingElementProxy = IFCInstanceExporter.CreateBuildingElementProxy(exporterIFC, element, guid,
                      ownerHistory, localPlacement, representation, null);

                  productWrapper.AddElement(element, buildingElementProxy, placementSetter.LevelInfo, ecData, true);
               }
               tr.Commit();
            }
         }

         return buildingElementProxy;
      }

      /// <summary>
      /// Exports an element as building element proxy.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool Export(ExporterIFC exporterIFC, Element element,
          GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         bool exported = false;
         if (element == null || geometryElement == null)
            return exported;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            exported = (ExportBuildingElementProxy(exporterIFC, element, geometryElement, productWrapper) != null);
            if (exported)
               tr.Commit();
         }

         return exported;
      }
   }
}