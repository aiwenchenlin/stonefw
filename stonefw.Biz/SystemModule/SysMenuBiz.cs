﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using stonefw.Biz.BaseModule;
using stonefw.Dao.BaseModule;
using stonefw.Dao.SystemModule;
using stonefw.Entity.BaseModule;
using stonefw.Entity.Enum;
using stonefw.Entity.Extension;
using stonefw.Entity.SystemModule;
using stonefw.Utility;
using stonefw.Utility.EntityExpressions;

namespace stonefw.Biz.SystemModule
{
    public class SysMenuBiz
    {
        private SysMenuDao _dao;
        private SysMenuDao Dao
        {
            get { return _dao ?? (_dao = new SysMenuDao()); }
        }

        /// <summary>
        /// 获取菜单树
        /// </summary>
        public List<SysMenuEntity> GetSysMenuTree()
        {
            List<SysMenuEntity> list = GetSysMenuDetailList();
            GetMenuTree(ref list);
            return list;
        }
        /// <summary>
        /// 获取菜单列表
        /// </summary>
        public List<SysMenuEntity> GetSysMenuList()
        {
            List<SysMenuEntity> list = GetSysMenuDetailList();
            int position = 0;
            GetMenuList(ref list, ref position);
            return list;
        }

        public SysMenuEntity GetSysMenuEntity(int? menuId)
        {
            var list = GetSysMenuDetailList(menuId);
            return list.Count > 0 ? list[0] : null;
        }
        public List<SysMenuEntity> GetSysMenuListByFatherNode(int fatherNode = 0)
        {
            List<SysMenuEntity> list = EntityExecution.ReadEntityList2<SysMenuEntity>(n => n.DeleteFlag == false && n.FatherNode == fatherNode);
            list = list.OrderBy(n => n.Seq).ToList();
            return list;
        }
        public void AddNewSysMenu(SysMenuEntity entity)
        {
            //获取目标目录下菜单的数量
            if (entity.FatherNode == 0)
                entity.MenuLevel = 1;
            else
            {
                SysMenuEntity fatherNode = GetSysMenuEntity(entity.FatherNode);
                entity.MenuLevel = fatherNode.MenuLevel + 1;
            }
            entity.MenuId = null;
            entity.Seq = GetCountByFatherNode(entity.FatherNode) + 1;
            entity.DeleteFlag = false;
            EntityExecution.InsertEntity(entity);
        }
        public void UpdateSysMenu(SysMenuEntity entity, int orgFatherNode)
        {
            if (entity.FatherNode != orgFatherNode)
            {
                if (entity.FatherNode == 0)
                    entity.MenuLevel = 1;
                else
                {
                    SysMenuEntity fatherNode = GetSysMenuEntity(entity.FatherNode);
                    entity.MenuLevel = fatherNode.MenuLevel + 1;
                }
                entity.Seq = GetCountByFatherNode(entity.FatherNode) + 1;
            }
            EntityExecution.UpdateEntity(entity);
            if (entity.FatherNode != orgFatherNode)
            {
                Dao.SeqRecal();
            }
        }
        public ExcuteResultEnum DeleteSysMenu(int menuId)
        {
            if (GetCountByFatherNode(menuId) > 0)
                return ExcuteResultEnum.IsOccupied;

            SysMenuEntity entity = new SysMenuEntity() { MenuId = menuId, DeleteFlag = true };
            EntityExecution.UpdateEntity(entity);
            return ExcuteResultEnum.Success;
        }
        public int GetCountByFatherNode(int? fatherNode)
        {
            return EntityExecution.GetEntityCount2<SysMenuEntity>(n => n.FatherNode == fatherNode && n.DeleteFlag == false);
        }

        /// <summary>
        /// 调整位置
        /// </summary>
        public void RecalSeq(Hashtable ht)
        {
            if (ht == null || ht.Count <= 0)
                return;

            foreach (DictionaryEntry entry in ht)
            {
                Dao.UpdateSeq(int.Parse(entry.Key.ToString()), int.Parse(entry.Value.ToString()));
            }
        }
        public List<SysMenuEntity> GetEnabledSysMenuList()
        {
            var sysMenuList = GetSysMenuDetailList().Where(n => n.ActivityFlag == true).ToList();
            int position = 0;
            GetMenuList(ref sysMenuList, ref position);
            return sysMenuList;
        }
        public List<SysMenuEntity> GetEnabledSysMenuListByPermission(List<PermissionEntity> permissionList)
        {
            var sysMenuList = GetEnabledSysMenuList();

            //1、去除没有权限的菜单
            for (int i = sysMenuList.Count - 1; i >= 0; i--)
            {
                var sysMenuEntity = sysMenuList[i];
                if (sysMenuEntity.ModuleId != "" && sysMenuEntity.FuncPointId != "")
                {
                    var list = permissionList.Where(n => n.ModuleId == sysMenuEntity.ModuleId && n.FuncPointId == sysMenuEntity.FuncPointId).ToList();
                    if (list.Count <= 0)
                    {
                        sysMenuList.Remove(sysMenuEntity);
                    }
                    else
                    {
                        if (!list[0].PermissionList.Contains(SysPermsPointEnum.View.ToString()))
                        {
                            sysMenuList.Remove(sysMenuEntity);
                        }
                    }
                }
            }

            //2、去除没有菜单的目录
            for (int i = sysMenuList.Count - 1; i >= 0; i--)
            {
                var sysMenuEntity = sysMenuList[i];
                if (sysMenuEntity.PageUrl == "")
                {
                    if (sysMenuList.Count(n => n.FatherNode == sysMenuEntity.MenuId) <= 0)
                    {
                        sysMenuList.Remove(sysMenuEntity);
                    }
                }
            }

            return sysMenuList;
        }

        #region 私有方法

        private List<SysMenuEntity> GetSysMenuDetailList(int? menuId = null)
        {
            var listSysMenuEntity = Dao.GetSysMenuList(menuId);
            var listSysModuleEnumEntity = new SysModuleEnumBiz().GetSysModuleEnumList();
            var listSysFuncPointEnumEntity = new SysFuncPointEnumBiz().GetSysFuncPointEnumList();
            var query = from sysMenuEntity in listSysMenuEntity
                        join sysModuleEnumEntity in listSysModuleEnumEntity on sysMenuEntity.ModuleId equals sysModuleEnumEntity.Name into r1
                        from sysModuleEnumEntity in r1.DefaultIfEmpty()
                        join sysFuncPointEnumEntity in listSysFuncPointEnumEntity on sysMenuEntity.FuncPointId equals sysFuncPointEnumEntity.Name into r2
                        from sysFuncPointEnumEntity in r2.DefaultIfEmpty()
                        select new SysMenuEntity()
                        {
                            MenuId = sysMenuEntity.MenuId,
                            MenuName = sysMenuEntity.MenuName,
                            MenuLevel = sysMenuEntity.MenuLevel,
                            Seq = sysMenuEntity.Seq,
                            FatherNode = sysMenuEntity.FatherNode,
                            Description = sysMenuEntity.Description,
                            PageUrl = sysMenuEntity.PageUrl,
                            UrlParameter = sysMenuEntity.UrlParameter,
                            ActivityFlag = sysMenuEntity.ActivityFlag,
                            DeleteFlag = sysMenuEntity.DeleteFlag,
                            ModuleId = sysMenuEntity.ModuleId,
                            FuncPointId = sysMenuEntity.FuncPointId,
                            ModuleName = sysModuleEnumEntity == null ? string.Empty : sysModuleEnumEntity.Description,
                            FuncPointName = sysFuncPointEnumEntity == null ? string.Empty : sysFuncPointEnumEntity.Description,
                        };
            var list = query.ToList<SysMenuEntity>();
            return list;
        }

        /// <summary>
        /// 获取菜单树，递归生成树结构
        /// </summary>
        private void GetMenuTree(ref List<SysMenuEntity> listMain, List<SysMenuEntity> listCurrentLevel = null)
        {
            if (listMain == null || listMain.Count <= 0)
                return;

            if (listCurrentLevel == null)
            {
                listCurrentLevel = listMain.Where<SysMenuEntity>(n => n.MenuLevel == 1).ToList();
            }

            if (listCurrentLevel.Count > 0)
            {
                for (int i = 0; i < listCurrentLevel.Count; i++)
                {
                    SysMenuEntity e = listCurrentLevel[i];
                    List<SysMenuEntity> listSubMenu = listMain.Where(n => n.FatherNode == e.MenuId).ToList();
                    if (listSubMenu.Count > 0)
                        GetMenuTree(ref listMain, listSubMenu);

                    //生成树结构，把当前节点加入到上层节点的子节点中。
                    if (e.MenuLevel > 1)
                    {
                        SysMenuEntity fatherNode = listMain.Where(n => n.MenuId == e.FatherNode).ToList()[0];
                        int index = listMain.IndexOf(fatherNode);
                        if (listMain[index].SubMenuList == null)
                            listMain[index].SubMenuList = new List<SysMenuEntity>();
                        listMain[index].SubMenuList.Add(e);
                        listMain.Remove(listMain[listMain.IndexOf(e)]);
                    }
                }
            }
        }
        /// <summary>
        /// 获取菜单列表，递归生成树名称和调整菜单位置
        /// </summary>
        private void GetMenuList(ref List<SysMenuEntity> listMain, ref int position, List<SysMenuEntity> listCurrentLevel = null, string skipLevel = "")
        {
            if (listMain == null || listMain.Count <= 0)
                return;

            if (listCurrentLevel == null)
            {
                listCurrentLevel = listMain.Where<SysMenuEntity>(n => n.MenuLevel == 1).ToList();
            }

            const string sign1 = "║ ";
            const string sign2 = "╠═";
            const string sign3 = "╚═";

            if (listCurrentLevel.Count > 0)
            {
                for (int i = 0; i < listCurrentLevel.Count; i++)
                {
                    SysMenuEntity e = listCurrentLevel[i];

                    //生成树名称
                    string treeName = string.Empty;
                    if (e.MenuLevel > 1)
                    {
                        for (int j = 0; j < e.MenuLevel - 1; j++)
                        {
                            if (skipLevel.Contains((j + 1).ToString()))
                                treeName += "　";
                            else
                                treeName += sign1;
                        }
                    }

                    if (i == listCurrentLevel.Count - 1)
                        treeName += sign3;
                    else
                        treeName += sign2;

                    int index = listMain.IndexOf(e);
                    e.MenuTreeName = treeName + e.MenuName;

                    //调整菜单位置
                    if (index == position)
                        listMain[position] = e;
                    else
                    {
                        var temp = listMain[position];
                        listMain[position] = e;
                        listMain[index] = temp;
                    }

                    //当父节点的最后的节点有子菜单，则该节点不需要添加竖线
                    if (e.Seq == listCurrentLevel.Count)
                        skipLevel += e.MenuLevel.ToString();

                    //标识当前节点的位置
                    position++;

                    List<SysMenuEntity> listSubMenu = listMain.Where(n => n.FatherNode == e.MenuId).ToList();
                    if (listSubMenu.Count > 0)
                        GetMenuList(ref listMain, ref position, listSubMenu, skipLevel);
                }
            }
        }

        private List<SysMenuEntity> GetMenuListWithChild(int? id, List<SysMenuEntity> list)
        {
            var listMenu = new List<SysMenuEntity>();
            listMenu.AddRange(list.Where(n => n.MenuId == id).ToList());

            var listSub = list.Where(n => n.FatherNode == id).ToList();
            if (listSub.Count > 0)
            {
                foreach (SysMenuEntity sub in listSub)
                {
                    listMenu.AddRange(GetMenuListWithChild(sub.MenuId, list));
                }
            }
            return listMenu;
        }
        /// <summary>
        /// 递归获取父菜单
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<SysMenuEntity> GetFatherMenuList(List<SysMenuEntity> list)
        {
            var result = new List<SysMenuEntity>();
            var father = new List<SysMenuEntity>();
            foreach (SysMenuEntity entity in list)
            {
                if (result.Count(n => n.MenuId == entity.MenuId) <= 0)
                    result.Add(entity);

                if (entity.MenuLevel != 1)
                {
                    var fatherNode = GetSysMenuEntity(entity.FatherNode);
                    if (fatherNode != null)
                    {
                        if (father.Count(n => n.MenuId == fatherNode.MenuId) <= 0)
                            father.Add(fatherNode);
                    }
                }
            }
            if (father.Count > 0)
            {
                var fatherList = GetFatherMenuList(father);
                if (fatherList != null && fatherList.Count > 0)
                {
                    foreach (SysMenuEntity entity in fatherList)
                    {
                        if (result.Count(n => n.MenuId == entity.MenuId) <= 0)
                            result.Add(entity);
                    }
                }
            }
            return result;
        }

        #endregion

    }
}

