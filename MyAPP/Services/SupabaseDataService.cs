using Sys = System;
using Net = System.Net.Http;
using Json = System.Text.Json;
using Tasks = System.Threading.Tasks;
using Coll = System.Collections.Generic;
using Models = MyAPP.Models;
using IO = System.IO;
using Linq = System.Linq;
using Http = System.Net.Http;
using Headers = System.Net.Http.Headers;

namespace MyAPP.Services
{
    public sealed class SupabaseDataService
    {
        private const Sys.String SUPABASE_URL = "https://owonxxmsnfsruymmlzga.supabase.co";
        private const Sys.String SUPABASE_ANON_KEY = "sb_publishable_JmWdf5RLIbStzNiN06vsMw_owqrDffB";
        private static readonly Net.HttpClient _sharedClient = new Net.HttpClient();

        private static readonly Json.JsonSerializerOptions _jsonOptions = new Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private void AddAuthHeaders(Http.HttpRequestMessage request)
        {
            Sys.String? token = AuthState.GetToken();

            if (Sys.String.IsNullOrWhiteSpace(token))
            {
                throw new Sys.InvalidOperationException("User not authenticated. Token is missing or empty.");
            }

            request.Headers.TryAddWithoutValidation("apikey", SUPABASE_ANON_KEY);
            request.Headers.Authorization = new Headers.AuthenticationHeaderValue("Bearer", token);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
        }

        public async Tasks.Task<Coll.List<Models.Preset>> GetPresetsWithDetailsAsync()
        {
            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/presets?select=*,templates(*,variables(*))&order=created_at.asc";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Get, endpoint);
            this.AddAuthHeaders(request);
            request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to fetch presets: ", error));
            }

            Sys.String json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Coll.List<Models.Preset>? presets = Json.JsonSerializer.Deserialize<Coll.List<Models.Preset>>(json, _jsonOptions);

            return presets ?? new Coll.List<Models.Preset>();
        }

        public async Tasks.Task<Coll.List<Models.Preset>> GetPresetsAsync()
        {
            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/presets?select=*&order=created_at.asc";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Get, endpoint);
            this.AddAuthHeaders(request);

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to fetch presets: ", error));
            }

            Sys.String json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Coll.List<Models.Preset>? presets = Json.JsonSerializer.Deserialize<Coll.List<Models.Preset>>(json, _jsonOptions);

            return presets ?? new Coll.List<Models.Preset>();
        }

        public async Tasks.Task<Models.Preset> CreatePresetAsync(Sys.String name)
        {
            if (Sys.String.IsNullOrWhiteSpace(name))
            {
                throw new Sys.ArgumentException("Preset name cannot be empty.");
            }

            Sys.String? token = AuthState.GetToken();
            Sys.Guid? userId = AuthState.GetUser(); 

            if (Sys.String.IsNullOrWhiteSpace(token))
            {
                throw new Sys.InvalidOperationException("Authentication token is missing. Please login again.");
            }

            if (userId == null)
            {
                throw new Sys.InvalidOperationException("User ID is missing. Please login again to refresh session.");
            }

            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/presets";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Post, endpoint);
            this.AddAuthHeaders(request);
            request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

            // PERBAIKAN UTAMA: Mengirimkan user_id dalam payload JSON
            var payload = new
            {
                name = name.Trim(),
                user_id = userId
            };

            Sys.String jsonPayload = Json.JsonSerializer.Serialize(payload);
            request.Content = new Http.StringContent(jsonPayload, Sys.Text.Encoding.UTF8, "application/json");

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);
            Sys.String responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.Console.WriteLine(Sys.String.Concat("Create Preset Error: ", response.StatusCode, " - ", responseJson));
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to create preset: ", responseJson));
            }

            Coll.List<Models.Preset>? created = Json.JsonSerializer.Deserialize<Coll.List<Models.Preset>>(responseJson, _jsonOptions);

            if (created == null || created.Count == 0)
            {
                throw new Sys.InvalidOperationException("Created preset returned empty.");
            }

            return created[0];
        }

        public async Tasks.Task UpdatePresetAsync(Models.Preset preset)
        {
            if (preset == null)
            {
                throw new Sys.ArgumentNullException(nameof(preset));
            }

            if (Sys.String.IsNullOrWhiteSpace(preset.Name))
            {
                throw new Sys.ArgumentException("Preset name cannot be empty.");
            }

            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/presets?id=eq.{preset.Id}";

            using var request = new Http.HttpRequestMessage(new Http.HttpMethod("PATCH"), endpoint);
            this.AddAuthHeaders(request);

            var payload = new { name = preset.Name.Trim() };
            Sys.String jsonPayload = Json.JsonSerializer.Serialize(payload);
            request.Content = new Http.StringContent(jsonPayload, Sys.Text.Encoding.UTF8, "application/json");

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to update preset: ", error));
            }
        }

        public async Tasks.Task DeletePresetAsync(Sys.Guid presetId)
        {
            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/presets?id=eq.{presetId}";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Delete, endpoint);
            this.AddAuthHeaders(request);

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to delete preset: ", error));
            }
        }

        public async Tasks.Task<Coll.List<Models.Template>> GetTemplatesByPresetAsync(Sys.Guid presetId)
        {
            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/templates?preset_id=eq.{presetId}&select=*&order=created_at.asc";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Get, endpoint);
            this.AddAuthHeaders(request);

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to fetch templates: ", error));
            }

            Sys.String json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Coll.List<Models.Template>? templates = Json.JsonSerializer.Deserialize<Coll.List<Models.Template>>(json, _jsonOptions);

            return templates ?? new Coll.List<Models.Template>();
        }

        public async Tasks.Task<Models.Template> CreateTemplateAsync(Sys.Guid presetId, Sys.String title, Sys.String content)
        {
            if (Sys.String.IsNullOrWhiteSpace(title))
            {
                throw new Sys.ArgumentException("Template title cannot be empty.");
            }

            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/templates";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Post, endpoint);
            this.AddAuthHeaders(request);
            request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

            var payload = new
            {
                preset_id = presetId,
                title = title.Trim(),
                content = content ?? Sys.String.Empty
            };
            Sys.String jsonPayload = Json.JsonSerializer.Serialize(payload);
            request.Content = new Http.StringContent(jsonPayload, Sys.Text.Encoding.UTF8, "application/json");

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);
            Sys.String responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to create template: ", responseJson));
            }

            Coll.List<Models.Template>? created = Json.JsonSerializer.Deserialize<Coll.List<Models.Template>>(responseJson, _jsonOptions);

            if (created == null || created.Count == 0)
            {
                throw new Sys.InvalidOperationException("Created template returned empty.");
            }

            return created[0];
        }

        public async Tasks.Task UpdateTemplateAsync(Models.Template template)
        {
            if (template == null)
            {
                throw new Sys.ArgumentNullException(nameof(template));
            }

            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/templates?id=eq.{template.Id}";

            using var request = new Http.HttpRequestMessage(new Http.HttpMethod("PATCH"), endpoint);
            this.AddAuthHeaders(request);

            var payload = new
            {
                title = template.Title.Trim(),
                content = template.Content
            };
            Sys.String jsonPayload = Json.JsonSerializer.Serialize(payload);
            request.Content = new Http.StringContent(jsonPayload, Sys.Text.Encoding.UTF8, "application/json");

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to update template: ", error));
            }
        }

        public async Tasks.Task DeleteTemplateAsync(Sys.Guid templateId)
        {
            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/templates?id=eq.{templateId}";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Delete, endpoint);
            this.AddAuthHeaders(request);

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to delete template: ", error));
            }
        }

        public async Tasks.Task<Coll.List<Models.Variable>> GetVariablesByTemplateAsync(Sys.Guid templateId)
        {
            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/variables?template_id=eq.{templateId}&select=*&order=created_at.asc";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Get, endpoint);
            this.AddAuthHeaders(request);

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to fetch variables: ", error));
            }

            Sys.String json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Coll.List<Models.Variable>? variables = Json.JsonSerializer.Deserialize<Coll.List<Models.Variable>>(json, _jsonOptions);

            return variables ?? new Coll.List<Models.Variable>();
        }

        public async Tasks.Task<Models.Variable> CreateVariableAsync(Sys.Guid templateId, Sys.String name, Sys.String? value)
        {
            if (Sys.String.IsNullOrWhiteSpace(name))
            {
                throw new Sys.ArgumentException("Variable name cannot be empty.");
            }

            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/variables";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Post, endpoint);
            this.AddAuthHeaders(request);
            request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

            var payload = new
            {
                template_id = templateId,
                name = name.Trim(),
                value = value
            };
            Sys.String jsonPayload = Json.JsonSerializer.Serialize(payload);
            request.Content = new Http.StringContent(jsonPayload, Sys.Text.Encoding.UTF8, "application/json");

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);
            Sys.String responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to create variable: ", responseJson));
            }

            Coll.List<Models.Variable>? created = Json.JsonSerializer.Deserialize<Coll.List<Models.Variable>>(responseJson, _jsonOptions);

            if (created == null || created.Count == 0)
            {
                throw new Sys.InvalidOperationException("Created variable returned empty.");
            }

            return created[0];
        }

        public async Tasks.Task UpdateVariableAsync(Models.Variable variable)
        {
            if (variable == null)
            {
                throw new Sys.ArgumentNullException(nameof(variable));
            }

            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/variables?id=eq.{variable.Id}";

            using var request = new Http.HttpRequestMessage(new Http.HttpMethod("PATCH"), endpoint);
            this.AddAuthHeaders(request);

            var payload = new
            {
                name = variable.Name.Trim(),
                value = variable.Value
            };
            Sys.String jsonPayload = Json.JsonSerializer.Serialize(payload);
            request.Content = new Http.StringContent(jsonPayload, Sys.Text.Encoding.UTF8, "application/json");

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to update variable: ", error));
            }
        }

        public async Tasks.Task DeleteVariableAsync(Sys.Guid variableId)
        {
            Sys.String endpoint = $"{SUPABASE_URL}/rest/v1/variables?id=eq.{variableId}";

            using var request = new Http.HttpRequestMessage(Http.HttpMethod.Delete, endpoint);
            this.AddAuthHeaders(request);

            using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode == false)
            {
                Sys.String error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Sys.InvalidOperationException(Sys.String.Concat("Failed to delete variable: ", error));
            }
        }

        public async Tasks.Task ExportPresetsToJsonAsync(Sys.String filePath)
        {
            Coll.List<Models.Preset> presets = await this.GetPresetsWithDetailsAsync().ConfigureAwait(false);

            var options = new Json.JsonSerializerOptions
            {
                WriteIndented = true
            };

            Sys.String json = Json.JsonSerializer.Serialize(presets, options);

            using var stream = new IO.FileStream(filePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.None);
            using var writer = new IO.StreamWriter(stream, Sys.Text.Encoding.UTF8);

            await writer.WriteAsync(json).ConfigureAwait(false);
        }

        public async Tasks.Task ImportPresetsFromJsonAsync(Sys.String filePath)
        {
            Sys.String json;

            using var stream = new IO.FileStream(filePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read);
            using var reader = new IO.StreamReader(stream, Sys.Text.Encoding.UTF8);

            json = await reader.ReadToEndAsync().ConfigureAwait(false);

            Coll.List<Models.Preset>? presets = Json.JsonSerializer.Deserialize<Coll.List<Models.Preset>>(json, _jsonOptions);

            if (presets == null)
            {
                throw new Sys.InvalidOperationException("Invalid JSON format or empty file.");
            }

            foreach (Models.Preset preset in presets)
            {
                Models.Preset newPreset = await this.CreatePresetAsync(preset.Name).ConfigureAwait(false);

                if (preset.Templates != null)
                {
                    foreach (Models.Template template in preset.Templates)
                    {
                        Models.Template newTemplate = await this.CreateTemplateAsync(
                            newPreset.Id,
                            template.Title,
                            template.Content
                        ).ConfigureAwait(false);

                        if (template.Variables != null)
                        {
                            foreach (Models.Variable variable in template.Variables)
                            {
                                await this.CreateVariableAsync(
                                    newTemplate.Id,
                                    variable.Name,
                                    variable.Value
                                ).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }
    }
}