﻿using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Web.Services;
using Yahoo.Yui.Compressor;

namespace WebVella.Erp.Web.TagHelpers
{
	[HtmlTargetElement("wv-field-textarea")]
	[RestrictChildren("wv-field-prepend", "wv-field-append")]
	public class WvFieldTextarea : WvFieldBase
	{

		[HtmlAttributeName("height")]
		public string Height { get; set; } = "";

		[HtmlAttributeName("autogrow")]
		public bool Autogrow { get; set; } = false;

		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			if (!isVisible)
			{
				output.SuppressOutput();
				return;
			}
			#region << Init >>
			var initSuccess = InitField(context, output);

			if (!initSuccess)
			{
				return;
			}

			#region << Init Prepend and Append >>
			var content = await output.GetChildContentAsync();
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(content.GetContent());
			var prependTaghelper = htmlDoc.DocumentNode.Descendants("wv-field-prepend");
			var appendTagHelper = htmlDoc.DocumentNode.Descendants("wv-field-append");

			foreach (var node in prependTaghelper)
			{
				PrependHtml.Add(node.InnerHtml.ToString());
			}

			foreach (var node in appendTagHelper)
			{
				AppendHtml.Add(node.InnerHtml.ToString());
			}

			#endregion

			#endregion

			#region << Render >>
			if (Mode == FieldRenderMode.Form)
			{
				var inputGroupEl = new TagBuilder("div");
				inputGroupEl.AddCssClass("input-group");
				//Prepend
				if (PrependHtml.Count > 0)
				{
					var prependEl = new TagBuilder("span");
					prependEl.AddCssClass($"input-group-prepend  erp-multilinetext {(ValidationErrors.Count > 0 ? "is-invalid" : "")}");
					foreach (var htmlString in PrependHtml)
					{
						prependEl.InnerHtml.AppendHtml(htmlString);
					}
					inputGroupEl.InnerHtml.AppendHtml(prependEl);
				}
				//Control
				var inputEl = new TagBuilder("textarea");
				var inputElCssClassList = new List<string>();
				inputElCssClassList.Add("form-control erp-multilinetext");
				if (Autogrow) {
					inputElCssClassList.Add("autogrow");
				}
				if (!String.IsNullOrWhiteSpace(Height)) {
					inputEl.Attributes.Add("style", $"height:{Height};");
				}
				inputEl.InnerHtml.AppendHtml((Value ?? "").ToString());
				inputEl.Attributes.Add("id", $"textarea-{FieldId}");
				inputEl.Attributes.Add("name", Name);
				if (Access == FieldAccess.Full || Access == FieldAccess.FullAndCreate)
				{
					if (Required)
					{
						inputEl.Attributes.Add("required", null);
					}
					if (!String.IsNullOrWhiteSpace(Placeholder))
					{
						inputEl.Attributes.Add("placeholder", Placeholder);
					}
				}
				else if (Access == FieldAccess.ReadOnly)
				{
					inputEl.Attributes.Add("readonly", null);
				}

				if (ValidationErrors.Count > 0)
				{
					inputElCssClassList.Add("is-invalid");
				}

				inputEl.Attributes.Add("class", String.Join(' ', inputElCssClassList));
				inputGroupEl.InnerHtml.AppendHtml(inputEl);
				//Append
				if (AppendHtml.Count > 0)
				{
					var appendEl = new TagBuilder("span");
					appendEl.AddCssClass($"input-group-append  erp-multilinetext {(ValidationErrors.Count > 0 ? "is-invalid" : "")}");

					foreach (var htmlString in AppendHtml)
					{
						appendEl.InnerHtml.AppendHtml(htmlString);
					}
					inputGroupEl.InnerHtml.AppendHtml(appendEl);
				}
				output.Content.AppendHtml(inputGroupEl);

				if (Autogrow)
				{
					var jsCompressor = new JavaScriptCompressor();
					#region << Add Autogrow Init >>
					var initScript = new TagBuilder("script");
					initScript.Attributes.Add("type", "text/javascript");
					var scriptTemplate = @"
						$(function(){
							new Autogrow(document.getElementById('textarea-{{FieldId}}'));
						});";
					scriptTemplate = scriptTemplate.Replace("{{FieldId}}", (FieldId ?? null).ToString());

					initScript.InnerHtml.AppendHtml(jsCompressor.Compress(scriptTemplate));
					output.PostContent.AppendHtml(initScript);
				}
				#endregion

			}
			else if (Mode == FieldRenderMode.Display)
			{

				if (!String.IsNullOrWhiteSpace(Value))
				{
					var divEl = new TagBuilder("div");
					divEl.Attributes.Add("id", $"input-{FieldId}");
					divEl.AddCssClass("form-control-plaintext erp-multilinetext");
					var textLines = (Value ?? "").ToString().Split(Environment.NewLine);
					foreach (var newLine in textLines)
					{
						var nlDivEl = new TagBuilder("div");
						nlDivEl.InnerHtml.Append(newLine);
						divEl.InnerHtml.AppendHtml(nlDivEl);
					}
					output.Content.AppendHtml(divEl);
				}
				else
				{
					output.Content.AppendHtml(EmptyValEl);
				}
			}
			else if (Mode == FieldRenderMode.Simple)
			{
				output.SuppressOutput();
				output.Content.Append((Value ?? "").ToString());
				return;
			}
			else if (Mode == FieldRenderMode.InlineEdit)
			{
				if (Access == FieldAccess.Full || Access == FieldAccess.FullAndCreate)
				{
					#region << View Wrapper >>
					{
						var viewWrapperEl = new TagBuilder("div");
						viewWrapperEl.AddCssClass("input-group view-wrapper");
						viewWrapperEl.Attributes.Add("title", "double click to edit");
						viewWrapperEl.Attributes.Add("id", $"view-{FieldId}");
						//Prepend
						if (PrependHtml.Count > 0)
						{
							var viewInputPrepend = new TagBuilder("span");
							viewInputPrepend.AddCssClass("input-group-prepend  erp-multilinetext");
							foreach (var htmlString in PrependHtml)
							{
								viewInputPrepend.InnerHtml.AppendHtml(htmlString);
							}
							viewWrapperEl.InnerHtml.AppendHtml(viewInputPrepend);
						}
						//Control
						var viewFormControlEl = new TagBuilder("textarea");
						viewFormControlEl.AddCssClass("form-control erp-multilinetext");
						if (!String.IsNullOrWhiteSpace(Height))
						{
							viewFormControlEl.Attributes.Add("style", $"height:{Height};");
						}
						viewFormControlEl.Attributes.Add("readonly", null);
						viewFormControlEl.InnerHtml.AppendHtml((Value ?? "").ToString());
						viewWrapperEl.InnerHtml.AppendHtml(viewFormControlEl);

						//Append
						var viewInputActionEl = new TagBuilder("span");
						viewInputActionEl.AddCssClass("input-group-append erp-multilinetext");
						foreach (var htmlString in AppendHtml)
						{
							viewInputActionEl.InnerHtml.AppendHtml(htmlString);
						}
						viewInputActionEl.InnerHtml.AppendHtml("<button type=\"button\" class='btn btn-white action' title='edit'><i class='fa fa-fw fa-pencil-alt'></i></button>");
						viewWrapperEl.InnerHtml.AppendHtml(viewInputActionEl);

						output.Content.AppendHtml(viewWrapperEl);
					}
					#endregion

					#region << Edit Wrapper>>
					{
						var editWrapperEl = new TagBuilder("div");
						editWrapperEl.Attributes.Add("id", $"edit-{FieldId}");
						editWrapperEl.Attributes.Add("style", $"display:none;");
						editWrapperEl.AddCssClass("edit-wrapper");

						var editInputGroupEl = new TagBuilder("div");
						editInputGroupEl.AddCssClass("input-group");
						//Prepend
						if (PrependHtml.Count > 0)
						{
							var editInputPrepend = new TagBuilder("span");
							editInputPrepend.AddCssClass("input-group-prepend  erp-multilinetext");
							foreach (var htmlString in PrependHtml)
							{
								editInputPrepend.InnerHtml.AppendHtml(htmlString);
							}
							editInputGroupEl.InnerHtml.AppendHtml(editInputPrepend);
						}
						//Control

						var editInputEl = new TagBuilder("textarea");
						editInputEl.AddCssClass("form-control erp-multilinetext");
						if (Autogrow)
						{
							editInputEl.AddCssClass("autogrow");
						}
						if (!String.IsNullOrWhiteSpace(Height))
						{
							editInputEl.Attributes.Add("style", $"height:{Height};");
						}
						if (Required)
						{
							editInputEl.Attributes.Add("required", null);
						}
						if (!String.IsNullOrWhiteSpace(Placeholder))
						{
							editInputEl.Attributes.Add("placeholder", Placeholder);
						}
						editInputEl.InnerHtml.AppendHtml((Value ?? "").ToString());
						editInputGroupEl.InnerHtml.AppendHtml(editInputEl);



						//Append
						var editInputGroupAppendEl = new TagBuilder("span");
						editInputGroupAppendEl.AddCssClass("input-group-append erp-multilinetext");

						foreach (var htmlString in AppendHtml)
						{
							editInputGroupAppendEl.InnerHtml.AppendHtml(htmlString);
						}
						editInputGroupAppendEl.InnerHtml.AppendHtml("<button type=\"button\" class='btn btn-white save' title='save'><i class='fa fa-fw fa-check go-green'></i></button>");
						editInputGroupAppendEl.InnerHtml.AppendHtml("<button type=\"button\" class='btn btn-white cancel' title='cancel'><i class='fa fa-fw fa-times go-gray'></i></button>");

						editInputGroupEl.InnerHtml.AppendHtml(editInputGroupAppendEl);
						editWrapperEl.InnerHtml.AppendHtml(editInputGroupEl);

						output.Content.AppendHtml(editWrapperEl);
					}
					#endregion

					var jsCompressor = new JavaScriptCompressor();
					#region << Init Scripts >>
					var tagHelperInitialized = false;
					if (ViewContext.HttpContext.Items.ContainsKey(typeof(WvFieldTextarea) + "-inline-edit"))
					{
						var tagHelperContext = (WvTagHelperContext)ViewContext.HttpContext.Items[typeof(WvFieldTextarea) + "-inline-edit"];
						tagHelperInitialized = tagHelperContext.Initialized;
					}
					if (!tagHelperInitialized)
					{
						var scriptContent = FileService.GetEmbeddedTextResource("inline-edit.js", "WebVella.Erp.Web.TagHelpers.WvFieldTextarea");
						var scriptEl = new TagBuilder("script");
						scriptEl.Attributes.Add("type", "text/javascript");
						scriptEl.InnerHtml.AppendHtml(jsCompressor.Compress(scriptContent));
						output.PostContent.AppendHtml(scriptEl);

						ViewContext.HttpContext.Items[typeof(WvFieldTextarea) + "-inline-edit"] = new WvTagHelperContext()
						{
							Initialized = true
						};

					}
					#endregion

					#region << Add Inline Init Script for this instance >>
					var initScript = new TagBuilder("script");
					initScript.Attributes.Add("type", "text/javascript");
					var scriptTemplate = @"
						$(function(){
							MultiLineTextInlineEditInit(""{{FieldId}}"",""{{Name}}"",""{{EntityName}}"",""{{RecordId}}"",{{ConfigJson}});
						});";
					scriptTemplate = scriptTemplate.Replace("{{FieldId}}", (FieldId ?? null).ToString());
					scriptTemplate = scriptTemplate.Replace("{{Name}}", Name);
					scriptTemplate = scriptTemplate.Replace("{{EntityName}}", EntityName);
					scriptTemplate = scriptTemplate.Replace("{{RecordId}}", (RecordId ?? null).ToString());

					var fieldConfig = new WvFieldTextareaConfig()
					{
						ApiUrl = ApiUrl,
						CanAddValues = Access == FieldAccess.FullAndCreate ? true : false
					};

					scriptTemplate = scriptTemplate.Replace("{{ConfigJson}}", JsonConvert.SerializeObject(fieldConfig));

					initScript.InnerHtml.AppendHtml(jsCompressor.Compress(scriptTemplate));

					output.PostContent.AppendHtml(initScript);
					#endregion
				}
				else if (Access == FieldAccess.ReadOnly)
				{

					var divEl = new TagBuilder("div");
					divEl.AddCssClass("input-group");
					//Prepend
					if (PrependHtml.Count > 0)
					{
						var viewInputPrepend = new TagBuilder("span");
						viewInputPrepend.AddCssClass("input-group-prepend erp-multilinetext");
						foreach (var htmlString in PrependHtml)
						{
							viewInputPrepend.InnerHtml.AppendHtml(htmlString);
						}
						divEl.InnerHtml.AppendHtml(viewInputPrepend);
					}
					//Control
					var inputEl = new TagBuilder("textarea");
					inputEl.AddCssClass("form-control erp-multilinetext");
					inputEl.Attributes.Add("type", "text");
					inputEl.InnerHtml.AppendHtml((Value ?? "").ToString());
					inputEl.Attributes.Add("readonly", null);
					divEl.InnerHtml.AppendHtml(inputEl);
					//Append
					var appendActionSpan = new TagBuilder("span");
					appendActionSpan.AddCssClass("input-group-append erp-multilinetext");
					foreach (var htmlString in AppendHtml)
					{
						appendActionSpan.InnerHtml.AppendHtml(htmlString);
					}
					appendActionSpan.InnerHtml.AppendHtml("<button type=\"button\" disabled class='btn btn-white action' title='locked'><i class='fa fa-fw fa-lock'></i></button>");
					divEl.InnerHtml.AppendHtml(appendActionSpan);

					output.Content.AppendHtml(divEl);
				}
			}
			#endregion


			//Finally
			if (SubInputEl != null)
			{
				output.PostContent.AppendHtml(SubInputEl);
			}

			return;
		}

	}
}
