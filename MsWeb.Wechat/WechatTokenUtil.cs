﻿using Mingshu.Framework.MSWeb.Core.AspectX;
using Mingshu.Framework.MSWeb.EFRepository;
using MsWeb.Core.Utils;
using MsWeb.DataObjects;
using MsWeb.DataObjects.Model;
using MsWeb.Domains;
using MsWeb.IServices;
using MsWeb.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Senparc.Weixin.MP.Containers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsWeb.Wechat
{
    public class WechatTokenUtil
    {
        public static string appid = "wx8773baee612e9144";
        public static string secret = "c7254c2be73a60f7e9408ebb91735c53";

        private static IWechatTokenService _Service;
        public static IWechatTokenService Service
        {
            get
            {
                if (_Service == null)
                {
                    IRepositoryContext context = new RepositoryContext();
                    IRepository<WechatToken> repository = new EfRepository<WechatToken>(context);
                    _Service = new WechatTokenService(context, repository);
                }
                return _Service;
            }
        }
        public static async Task<string> GetWechateToken()
        {
            string token = string.Empty; 
            ReturnResult<WechatTokenModel> resultToken = await Service.GetWechatToken();
            //如果为空，则通过微信接口获取Token,保存本地，同时返回
            if (resultToken.data == null || string.IsNullOrEmpty(resultToken.data.ID))
            {
                string tokenJson = WechatHttpClientUtil.dooGet(
                    string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}",
                    appid,secret));
                JObject jo = (JObject)JsonConvert.DeserializeObject(tokenJson);
                token = jo["access_token"].ToString();
                WechatTokenModel saveToken = new WechatTokenModel
                {
                    ID = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now,
                    updatetime = DateTime.Now,
                    token = token,
                };
                await Service.UpdateWechatToken(saveToken);//此处可以开线程执行
            }
            else
            {
                token = resultToken.data.token;
            }
            return token;
        }

        public static string GetUserOpenId()
        {
            string openid = string.Empty;
            openid = WechatHttpClientUtil.dooGet(
                   string.Format("https://api.weixin.qq.com/cgi-bin/user/get?access_token={0}&next_openid=",
                   AccessTokenContainer.TryGetAccessToken(appid, secret)));
            return openid;
            
        }
        /// <summary>
        /// 发送模板消息给用户（测试账号已通）
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="template_id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool SendMessageToUser(string openid, string template_id,string targetUrl, string data)
        {
            bool isSuccess = true;
            string postUrl = string.Format("https://api.weixin.qq.com/cgi-bin/message/template/send?access_token={0}",
                AccessTokenContainer.TryGetAccessToken(appid, secret));
            //var result = ;
            LogUtil.WebLog("postUrl:" + postUrl);
            //{{User.DATA}}您的订单{{Goods.DATA}}已发送成功，点击可查看订单详情！
            //模板ID：PwSL3zRR1BvqeV74VCHVeRrKr_VVqpHoVtMsetUB65c
            //openid:ogXxw0uil2siqIvGLne_ATt0SufI
            /*
             {
                "touser": "o-HDs1CyHSDOM8-XvMe0g4mvXHio", 
                "template_id": "seqcZG4-OgC_OkJkPctcfE9EC7kGuVbZpvCf4AaXm_s", 
                "url": "http://weixin.qq.com/download", 
                "topcolor": "#FF0000", 
                "data": {
                    "User": {
                        "value": "李先生", 
                        "color": "#173177"
                    }, 
                    "Date": {
                        "value": "06月07日 19时24分", 
                        "color": "#173177"
                    }, 
                    "Goods": {
                        "value": "图书", 
                        "color": "#173177"
                    }
                }
            }
             */
            try
            {
                //需要传入
                openid = "ogXxw0uil2siqIvGLne_ATt0SufI";
                template_id = "PwSL3zRR1BvqeV74VCHVeRrKr_VVqpHoVtMsetUB65c";
                targetUrl = "http://www.baidu.com";
                data = "{\"User\": {\"value\":\"李先生\",\"color\": \"#173177\"},"
                + "\"Date\":{\"value\":\"06月07日 19时24分\",\"color\": \"#173177\"},"
                + "\"Goods\":{\"value\":\"手术刀\",\"color\":\"#173177\"}}";
                LogUtil.WebLog("data:" + data);
                string postData = string.Format("{\"touser\":\"{0}\",", openid)
                    + string.Format("\"template_id\":\"{0}\",", template_id)
                    + string.Format("\"url\":\"{0}\",", targetUrl)
                    + "\"topcolor\":\"#FF0000\","
                    + string.Format("\"data\":{0}}", data);
                LogUtil.WebLog("postData:" + postData);
                if (WechatHttpClientUtil.dooPost(postUrl, postData, "json").ToUpper() != "TRUE")
                {
                    isSuccess = false;
                }
            }
            catch (Exception exp)
            {
                LogUtil.WebError(exp);
            }
            
            return isSuccess;
        }
    }
}
