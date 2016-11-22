﻿using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.UI.Paging;
using Nop.Web.Infrastructure.Cache;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace Nop.Web.Models.Catalog
{
	/// <summary>
	/// Filtering and paging model for catalog
	/// </summary>
	public partial class CatalogPagingFilteringModel : BasePageableModel
	{
		#region Ctor

		/// <summary>
		/// Constructor
		/// </summary>
		public CatalogPagingFilteringModel()
		{
			this.AvailableSortOptions = new List<SelectListItem>();
			this.AvailableViewModes = new List<SelectListItem>();
			this.PageSizeOptions = new List<SelectListItem>();
			this.PriceRangeFilter = new PriceRangeFilterModel();
			this.SpecificationFilter = new SpecificationFilterModel();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Price range filter model
		/// </summary>
		public PriceRangeFilterModel PriceRangeFilter { get; set; }
		/// <summary>
		/// Specification filter model
		/// </summary>
		public SpecificationFilterModel SpecificationFilter { get; set; }

		/// <summary>
		/// A value indicating whether product sorting is allowed
		/// </summary>
		public bool AllowProductSorting { get; set; }
		/// <summary>
		/// Available sort options
		/// </summary>
		public IList<SelectListItem> AvailableSortOptions { get; set; }

		/// <summary>
		/// A value indicating whether customers are allowed to change view mode
		/// </summary>
		public bool AllowProductViewModeChanging { get; set; }
		/// <summary>
		/// Available view mode options
		/// </summary>
		public IList<SelectListItem> AvailableViewModes { get; set; }

		/// <summary>
		/// A value indicating whether customers are allowed to select page size
		/// </summary>
		public bool AllowCustomersToSelectPageSize { get; set; }
		/// <summary>
		/// Available page size options
		/// </summary>
		public IList<SelectListItem> PageSizeOptions { get; set; }

		/// <summary>
		/// Order by
		/// </summary>
		public int? OrderBy { get; set; }

		/// <summary>
		/// Product sorting
		/// </summary>
		public string ViewMode { get; set; }

		#endregion

		#region Nested classes

		/// <summary>
		/// Price range filter model
		/// </summary>
		public partial class PriceRangeFilterModel : BaseNopModel
		{
			#region Const

			private const string QUERYSTRINGPARAM = "price";

			#endregion

			#region Ctor

			/// <summary>
			/// Constuctor
			/// </summary>
			public PriceRangeFilterModel()
			{
				this.Items = new List<PriceRangeFilterItem>();
			}

			#endregion

			#region Utilities

			/// <summary>
			/// Gets parsed price ranges
			/// </summary>
			/// <param name="priceRangesStr">Price ranges in string format</param>
			/// <returns>Price ranges</returns>
			protected virtual IList<PriceRange> GetPriceRangeList(string priceRangesStr)
			{
				var priceRanges = new List<PriceRange>();
				if (string.IsNullOrWhiteSpace(priceRangesStr))
					return priceRanges;
				string[] rangeArray = priceRangesStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string str1 in rangeArray)
				{
					string[] fromTo = str1.Trim().Split(new[] { '-' });

					decimal? from = null;
					if (!String.IsNullOrEmpty(fromTo[0]) && !String.IsNullOrEmpty(fromTo[0].Trim()))
						from = decimal.Parse(fromTo[0].Trim(), new CultureInfo("en-US"));

					decimal? to = null;
					if (!String.IsNullOrEmpty(fromTo[1]) && !String.IsNullOrEmpty(fromTo[1].Trim()))
						to = decimal.Parse(fromTo[1].Trim(), new CultureInfo("en-US"));

					priceRanges.Add(new PriceRange { From = from, To = to });
				}
				return priceRanges;
			}

			/// <summary>
			/// Exclude query string parameters
			/// </summary>
			/// <param name="url">URL</param>
			/// <param name="webHelper">Web helper</param>
			/// <returns>New URL</returns>
			protected virtual string ExcludeQueryStringParams(string url, IWebHelper webHelper)
			{
				//comma separated list of parameters to exclude
				const string excludedQueryStringParams = "pagenumber";
				var excludedQueryStringParamsSplitted = excludedQueryStringParams.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string exclude in excludedQueryStringParamsSplitted)
					url = webHelper.RemoveQueryString(url, exclude);
				return url;
			}

			#endregion

			#region Methods

			/// <summary>
			/// Get selected price range
			/// </summary>
			/// <param name="webHelper">Web helper</param>
			/// <param name="priceRangesStr">Price ranges in string format</param>
			/// <returns>Price ranges</returns>
			public virtual PriceRange GetSelectedPriceRange(IWebHelper webHelper, string priceRangesStr)
			{
				var range = webHelper.QueryString<string>(QUERYSTRINGPARAM);
				if (String.IsNullOrEmpty(range))
					return null;
				string[] fromTo = range.Trim().Split(new[] { '-' });
				if (fromTo.Length == 2)
				{
					decimal? from = null;
					if (!String.IsNullOrEmpty(fromTo[0]) && !String.IsNullOrEmpty(fromTo[0].Trim()))
						from = decimal.Parse(fromTo[0].Trim(), new CultureInfo("en-US"));
					decimal? to = null;
					if (!String.IsNullOrEmpty(fromTo[1]) && !String.IsNullOrEmpty(fromTo[1].Trim()))
						to = decimal.Parse(fromTo[1].Trim(), new CultureInfo("en-US"));

					var priceRangeList = GetPriceRangeList(priceRangesStr);
					foreach (var pr in priceRangeList)
					{
						if (pr.From == from && pr.To == to)
							return pr;
					}
				}
				return null;
			}

			/// <summary>
			/// Load price range filters
			/// </summary>
			/// <param name="priceRangeStr">Price range in string format</param>
			/// <param name="webHelper">Web helper</param>
			/// <param name="priceFormatter">Price formatter</param>
			public virtual void LoadPriceRangeFilters(string priceRangeStr, IWebHelper webHelper, IPriceFormatter priceFormatter)
			{
				var priceRangeList = GetPriceRangeList(priceRangeStr);
				if (priceRangeList.Any())
				{
					this.Enabled = true;

					var selectedPriceRange = GetSelectedPriceRange(webHelper, priceRangeStr);

					this.Items = priceRangeList.ToList().Select(x =>
					{
						//from&to
						var item = new PriceRangeFilterItem();
						if (x.From.HasValue)
							item.From = priceFormatter.FormatPrice(x.From.Value, true, false);
						if (x.To.HasValue)
							item.To = priceFormatter.FormatPrice(x.To.Value, true, false);
						string fromQuery = string.Empty;
						if (x.From.HasValue)
							fromQuery = x.From.Value.ToString(new CultureInfo("en-US"));
						string toQuery = string.Empty;
						if (x.To.HasValue)
							toQuery = x.To.Value.ToString(new CultureInfo("en-US"));

						//is selected?
						if (selectedPriceRange != null
							&& selectedPriceRange.From == x.From
							&& selectedPriceRange.To == x.To)
							item.Selected = true;

						//filter URL
						string url = webHelper.ModifyQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM + "=" + fromQuery + "-" + toQuery, null);
						url = ExcludeQueryStringParams(url, webHelper);
						item.FilterUrl = url;


						return item;
					}).ToList();

					if (selectedPriceRange != null)
					{
						//remove filter URL
						string url = webHelper.RemoveQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM);
						url = ExcludeQueryStringParams(url, webHelper);
						this.RemoveFilterUrl = url;
					}
				}
				else
				{
					this.Enabled = false;
				}
			}

			#endregion

			#region Properties

			/// <summary>
			/// Gets or sets a value indicating whether filtering is enabled
			/// </summary>
			public bool Enabled { get; set; }
			/// <summary>
			/// Filter items
			/// </summary>
			public IList<PriceRangeFilterItem> Items { get; set; }
			/// <summary>
			/// URL of "remove filters" button
			/// </summary>
			public string RemoveFilterUrl { get; set; }

			#endregion
		}

		/// <summary>
		/// Price range filter item
		/// </summary>
		public partial class PriceRangeFilterItem : BaseNopModel
		{
			/// <summary>
			/// From
			/// </summary>
			public string From { get; set; }
			/// <summary>
			/// To
			/// </summary>
			public string To { get; set; }
			/// <summary>
			/// Filter URL
			/// </summary>
			public string FilterUrl { get; set; }
			/// <summary>
			/// Is selected?
			/// </summary>
			public bool Selected { get; set; }
		}

		/// <summary>
		/// Specification filter model
		/// </summary>
		public partial class SpecificationFilterModel : BaseNopModel
		{
			#region Const

			private const string QUERYSTRINGPARAM = "specs";

			#endregion

			#region Ctor

			/// <summary>
			/// Ctor
			/// </summary>
			public SpecificationFilterModel()
			{
				this.AlreadyFilteredItems = new List<SpecificationFilterItem>();
				this.NotFilteredItems = new List<SpecificationFilterItem>();
			}

			#endregion

			#region Utilities

			/// <summary>
			/// Exclude query string parameters
			/// </summary>
			/// <param name="url">URL</param>
			/// <param name="webHelper">Web helper</param>
			/// <returns>New URL</returns>
			protected virtual string ExcludeQueryStringParams(string url, IWebHelper webHelper)
			{
				//comma separated list of parameters to exclude
				const string excludedQueryStringParams = "pagenumber";
				var excludedQueryStringParamsSplitted = excludedQueryStringParams.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string exclude in excludedQueryStringParamsSplitted)
					url = webHelper.RemoveQueryString(url, exclude);
				return url;
			}

			/// <summary>
			/// Generate URL of already filtered items
			/// </summary>
			/// <param name="optionIds">Option IDs</param>
			/// <returns>URL</returns>
			protected virtual string GenerateFilteredSpecQueryParam(IList<int> optionIds)
			{
				if (optionIds == null)
					return "";

				string result = string.Join(",", optionIds);
				return result;
			}

			#endregion

			#region Methods

			/// <summary>
			/// Get IDs of already filtered specification options
			/// </summary>
			/// <param name="webHelper">Web helper</param>
			/// <returns>IDs</returns>
			public virtual List<int> GetAlreadyFilteredSpecOptionIds(IWebHelper webHelper)
			{
				var result = new List<int>();

				var alreadyFilteredSpecsStr = webHelper.QueryString<string>(QUERYSTRINGPARAM);
				if (String.IsNullOrWhiteSpace(alreadyFilteredSpecsStr))
					return result;

				foreach (var spec in alreadyFilteredSpecsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					int specId;
					int.TryParse(spec.Trim(), out specId);
					if (!result.Contains(specId))
						result.Add(specId);
				}
				return result;
			}

			/// <summary>
			/// Prepare model
			/// </summary>
			/// <param name="alreadyFilteredSpecOptionIds">IDs of already filtered specification options</param>
			/// <param name="filterableSpecificationAttributeOptionIds">IDs of filterable specification options</param>
			/// <param name="specificationAttributeService"></param>
			/// <param name="webHelper">Web helper</param>
			/// <param name="workContext">Work context</param>
			/// <param name="cacheManager">Cache manager</param>
			public virtual void PrepareSpecsFilters(
				IList<int> alreadyFilteredSpecOptionIds,
				int[] filterableSpecificationAttributeOptionIds,
				ISpecificationAttributeService specificationAttributeService,
				IWebHelper webHelper,
				IWorkContext workContext,
				ICacheManager cacheManager)
			{
				Enabled = false;
				var optionIds = filterableSpecificationAttributeOptionIds != null
					? string.Join(",", filterableSpecificationAttributeOptionIds) : string.Empty;
				var cacheKey = string.Format(ModelCacheEventConsumer.SPECS_FILTER_MODEL_KEY, optionIds, workContext.WorkingLanguage.Id);

				var allOptions = specificationAttributeService.GetSpecificationAttributeOptionsByIds(filterableSpecificationAttributeOptionIds);
				var allFilters = cacheManager.Get(cacheKey, () => allOptions.Select(sao =>
					new SpecificationAttributeOptionFilter
					{
						SpecificationAttributeId = sao.SpecificationAttribute.Id,
						SpecificationAttributeName = sao.SpecificationAttribute.GetLocalized(x => x.Name, workContext.WorkingLanguage.Id),
						SpecificationAttributeDisplayOrder = sao.SpecificationAttribute.DisplayOrder,
						SpecificationAttributeOptionId = sao.Id,
						SpecificationAttributeOptionName = sao.GetLocalized(x => x.Name, workContext.WorkingLanguage.Id),
						SpecificationAttributeOptionColorRgb = sao.ColorSquaresRgb,
						SpecificationAttributeOptionDisplayOrder = sao.DisplayOrder
					}).ToList());

				if (!allFilters.Any())
					return;

				//sort loaded options
				allFilters = allFilters.OrderBy(saof => saof.SpecificationAttributeDisplayOrder)
					.ThenBy(saof => saof.SpecificationAttributeOptionDisplayOrder)
					.ToList();

				//prepare the model properties
				Enabled = true;
				var removeFilterUrl = webHelper.RemoveQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM);
				RemoveFilterUrl = ExcludeQueryStringParams(removeFilterUrl, webHelper);

				//get already filtered specification options
				var alreadyFilteredOptions = allFilters.Where(x => alreadyFilteredSpecOptionIds.Contains(x.SpecificationAttributeOptionId));

				//get not filtered specification options
				AllItems = allFilters.Select(x =>
				{
					//filter URL
					var currentIsSelected = alreadyFilteredSpecOptionIds.Contains(x.SpecificationAttributeOptionId);

					var alreadyFiltered = currentIsSelected ?
						alreadyFilteredSpecOptionIds.Except(new List<int> { x.SpecificationAttributeOptionId }) :
						alreadyFilteredSpecOptionIds.Concat(new List<int> { x.SpecificationAttributeOptionId });

					var filterSpecParamValue = GenerateFilteredSpecQueryParam(alreadyFiltered.ToList());
					var queryString = string.Format("{0}={1}", QUERYSTRINGPARAM, filterSpecParamValue);

					var filterUrl = string.IsNullOrWhiteSpace(filterSpecParamValue) ?
						RemoveFilterUrl :
						webHelper.ModifyQueryString(webHelper.GetThisPageUrl(true), queryString, null);

					return new SpecificationFilterItem()
					{
						SpecificationAttributeName = x.SpecificationAttributeName,
						SpecificationAttributeOptionName = x.SpecificationAttributeOptionName,
						SpecificationAttributeOptionColorRgb = x.SpecificationAttributeOptionColorRgb,
						FilterUrl = ExcludeQueryStringParams(filterUrl, webHelper),
						Name = x.SpecificationAttributeOptionId.ToString(),
						Selected = currentIsSelected
					};
				}).ToList();
			}

			#endregion

			#region Properties
			/// <summary>
			/// Gets or sets a value indicating whether filtering is enabled
			/// </summary>
			public bool Enabled { get; set; }
			/// <summary>
			/// Already filtered items
			/// </summary>
			public IList<SpecificationFilterItem> AlreadyFilteredItems { get; set; }
			/// <summary>
			/// Not filtered yet items
			/// </summary>
			public IList<SpecificationFilterItem> NotFilteredItems { get; set; }
			/// <summary>
			/// All items
			/// </summary>
			public IList<SpecificationFilterItem> AllItems { get; set; }
			/// <summary>
			/// URL of "remove filters" button
			/// </summary>
			public string RemoveFilterUrl { get; set; }

			#endregion
		}

		/// <summary>
		/// Specification filter item
		/// </summary>
		public partial class SpecificationFilterItem : BaseNopModel
		{
			/// <summary>
			/// Specification attribute name
			/// </summary>
			public string SpecificationAttributeName { get; set; }
			/// <summary>
			/// Specification attribute option name
			/// </summary>
			public string SpecificationAttributeOptionName { get; set; }
			/// <summary>
			/// Specification attribute option color (RGB)
			/// </summary>
			public string SpecificationAttributeOptionColorRgb { get; set; }
			/// <summary>
			/// Filter URL
			/// </summary>
			public string FilterUrl { get; set; }

			/// <summary>
			/// Name to define property on client side
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// Is Current option is selected
			/// </summary>
			public bool Selected { get; set; }
		}

		#endregion
	}
}