using LNF.CommonTools;
using LNF.Control;
using LNF.Models.Control;
using LNF.Repository;
using LNF.Repository.Control;
using LNF.WebApi.Control.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace LNF.WebApi.Control.Controllers
{
    public class DefaultController : ApiController
    {
        [Route("")]
        public string Get()
        {
            return "control-api";
        }

        [BasicAuthentication, Route("block")]
        public IEnumerable<BlockItem> GetBlocks()
        {
            return DA.Current.Query<Block>().Model<BlockItem>();
        }

        [BasicAuthentication, Route("block/{blockId}")]
        public async Task<BlockResponse> GetBlockState(int blockId)
        {
            Block block = DA.Current.Single<Block>(blockId);

            if (block == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            var blockResponse = await ServiceProvider.Current.Control.GetBlockState(block);
            return blockResponse;
        }

        [BasicAuthentication, HttpGet, Route("point/{pointId}/{state}")]
        public async Task<PointResponse> SetPointState(int pointId, string state, uint duration = 0)
        {
            var point = DA.Current.Single<Point>(pointId);

            if (point == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            var pointResponse = await ServiceProvider.Current.Control.SetPointState(point, GetState(state), duration);

            return pointResponse;
        }

        [BasicAuthentication, HttpGet, Route("action/{actionId}")]
        public IEnumerable<ActionInstanceItem> GetActionInstance(int actionId)
        {
            return DA.Current.Query<ActionInstance>().Where(x => x.ActionID == actionId).Model<ActionInstanceItem>();
        }

        [HttpGet, Route("status")]
        public async Task<IEnumerable<ToolStatus>> GetToolStatus()
        {
            DataTable dt = new DataTable();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["cnSselData"].ConnectionString))
            using (var cmd = new SqlCommand("SELECT * FROM sselControl.dbo.v_ToolStatus WHERE IsActive = 1 ORDER BY BuildingName, LabDisplayName, ProcessTechName, ResourceName", conn))
            using (var adap = new SqlDataAdapter(cmd))
            {
                adap.Fill(dt);
                conn.Close();
            }

            await WagoInterlock.AllToolStatus(dt);

            var result = dt.AsEnumerable().Select(x => new ToolStatus()
            {
                BuildingID = x.Field<int>("BuildingID"),
                BuildingName = x.Field<string>("BuildingName"),
                LabID = x.Field<int>("LabID"),
                LabName = x.Field<string>("LabName"),
                LabDisplayName = x.Field<string>("LabDisplayName"),
                ProcessTechID = x.Field<int>("ProcessTechID"),
                ProcessTechName = x.Field<string>("ProcessTechName"),
                ResourceID = x.Field<int>("ResourceID"),
                ResourceName = x.Field<string>("ResourceName"),
                PointID = x.Field<int>("PointID"),
                InterlockStatus = x.Field<string>("InterlockStatus"),
                InterlockState = x.Field<bool>("InterlockState"),
                InterlockError = x.Field<bool>("InterlockError"),
                IsInterlocked = x.Field<bool>("IsInterlocked")
            }).ToList();

            return result;
        }

        [BasicAuthentication, HttpGet, Route("action/{name}/{actionId}")]
        public async Task<PointState> GetPointState(string name, int actionId)
        {
            var act = DA.Current.Query<ActionInstance>().FirstOrDefault(x => x.ActionName.ToLower() == name.ToLower() && x.ActionID == actionId);

            if (act == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            var point = act.GetPoint();

            var blockResponse = await ServiceProvider.Current.Control.GetBlockState(point.Block);

            var pointState = blockResponse.BlockState.Points.First(x => x.PointID == act.Point);

            return pointState;
        }

        [BasicAuthentication, HttpGet, Route("action/{name}/{actionId}/{state}")]
        public async Task<PointResponse> SetPointState(string name, int actionId, string state, uint duration = 0)
        {
            var act = DA.Current.Query<ActionInstance>().FirstOrDefault(x => x.ActionName.ToLower() == name.ToLower() && x.ActionID == actionId);

            if (act == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            var point = act.GetPoint();

            var pointResponse = await ServiceProvider.Current.Control.SetPointState(point, GetState(state), 0);

            return pointResponse;
        }

        private bool GetState(string state)
        {
            if (state.ToLower() == "on")
                return true;
            if (state.ToLower() == "off")
                return false;
            else
                throw new ArgumentException("Invalid state. Allowed values are 'on' and 'off'.", "state");
        }
    }
}
