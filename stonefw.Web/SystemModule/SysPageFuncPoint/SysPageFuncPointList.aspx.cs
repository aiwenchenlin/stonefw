using System;
using System.Web.UI.WebControls;
using Stonefw.Biz.SystemModule;
using Stonefw.Web.Utility.BaseClass;

namespace Stonefw.Web.SystemModule.SysPageFuncPoint
{
    public partial class SysPageFuncPointList : BasePage
    {
        private SysPageFuncPointBiz _biz;

        private SysPageFuncPointBiz Biz
        {
            get { return _biz ?? (_biz = new SysPageFuncPointBiz()); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                BindData();
        }

        protected void btnQuery_Click(object sender, EventArgs e)
        {
            BindData();
            lMessage.Text = "执行成功！";
        }

        protected void gvSysPageFuncPoint_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Row_Delete")
            {
                var arg = e.CommandArgument.ToString().Split('|');
                Biz.DeleteSysPageFuncPoint(arg[0]);
                BindData();
            }
        }

        protected void gvSysPageFuncPoint_PageIndexChanged(object sender, EventArgs e)
        {
            BindData();
        }

        protected void gvSysPageFuncPoint_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvSysPageFuncPoint.PageIndex = e.NewPageIndex;
        }

        private void BindData()
        {
            gvSysPageFuncPoint.PageSize = int.Parse(SysGlobalSetting.GridViewPageSize);
            gvSysPageFuncPoint.DataSource = Biz.GetSysPageFuncPointList();
            gvSysPageFuncPoint.DataBind();
        }
    }
}