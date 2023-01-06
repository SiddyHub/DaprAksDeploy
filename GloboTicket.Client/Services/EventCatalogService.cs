using GloboTicket.Web.Extensions;
using GloboTicket.Web.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GloboTicket.Web.Services
{
    public class EventCatalogService : IEventCatalogService
    {        
        private readonly HttpClient _httpClient;               
        public EventCatalogService(HttpClient httpClient)
        {                        
            _httpClient = httpClient;            
        }

        public async Task<IEnumerable<Event>> GetAll()
        {            
            var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}/api/events");            
            return await response.ReadContentAs<List<Event>>();
        }

        public async Task<IEnumerable<Event>> GetByCategoryId(Guid categoryid)
        {                     
            var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}/api/events/?categoryId={categoryid}");
            return await response.ReadContentAs<List<Event>>();
        }

        public async Task<Event> GetEvent(Guid id)
        {                      
            var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}/api/events/{id}");
            return await response.ReadContentAs<Event>();
        }

        public async Task<IEnumerable<Category>> GetCategories()
        {            
            var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}/api/categories");            
            return await response.ReadContentAs<List<Category>>();
        }

        public async Task<PriceUpdate> UpdatePrice(PriceUpdate priceUpdate)
        {                     
            var response = await _httpClient.PostAsJson($"{_httpClient.BaseAddress}/api/events/eventpriceupdate", priceUpdate);
            return await response.ReadContentAs<PriceUpdate>();
        }        
    }
}
