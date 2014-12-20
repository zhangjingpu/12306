using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JasonLong.Helper
{
    public class JSFunctionHelper
    {
        public static string GetLoginRandomCodes(string key,string value)
        {
            string func = "function bin216(s){var i,l,o='',n;s+='';b='';for(i=0,l=s.length;i<l;i++){b=s.charCodeAt(i);n=b.toString(16);o+=n.length<2?'0'+n:n}return o};var Base32=new function(){var delta=2654435768;function longArrayToString(data,includeLength){var length=data.length;var n=(length-1)<<2;if(includeLength){var m=data[length-1];if((m<n-3)||(m>n))return null;n=m}for(var i=0;i<length;i++){data[i]=String.fromCharCode(data[i]&255,data[i]>>>8&255,data[i]>>>16&255,data[i]>>>24&255)}if(includeLength){return data.join('').substring(0,n)}else{return data.join('')}};function stringToLongArray(string,includeLength){var length=string.length;var result=[];for(var i=0;i<length;i+=4){result[i>>2]=string.charCodeAt(i)|string.charCodeAt(i+1)<<8|string.charCodeAt(i+2)<<16|string.charCodeAt(i+3)<<24}if(includeLength){result[result.length]=length}return result};this.encrypt=function(string,key){if(string==''){return''}var v=stringToLongArray(string,true);var k=stringToLongArray(key,false);if(k.length<4){k.length=4}var n=v.length-1;var z=v[n],y=v[0];var mx,e,p,q=Math.floor(6+52/(n+1)),sum=0;while(0<q--){sum=sum+delta&4294967295;e=sum>>>2&3;for(p=0;p<n;p++){y=v[p+1];mx=(z>>>5^y<<2)+(y>>>3^z<<4)^(sum^y)+(k[p&3^e]^z);z=v[p]=v[p]+mx&4294967295}y=v[0];mx=(z>>>5^y<<2)+(y>>>3^z<<4)^(sum^y)+(k[p&3^e]^z);z=v[n]=v[n]+mx&4294967295}return longArrayToString(v,false)}};var keyStr='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=';function encode32(input){input=escape(input);var output='';var chr1,chr2,chr3='';var enc1,enc2,enc3,enc4='';var i=0;do{chr1=input.charCodeAt(i++);chr2=input.charCodeAt(i++);chr3=input.charCodeAt(i++);enc1=chr1>>2;enc2=((chr1&3)<<4)|(chr2>>4);enc3=((chr2&15)<<2)|(chr3>>6);enc4=chr3&63;if(isNaN(chr2)){enc3=enc4=64}else if(isNaN(chr3)){enc4=64}output=output+keyStr.charAt(enc1)+keyStr.charAt(enc2)+keyStr.charAt(enc3)+keyStr.charAt(enc4);chr1=chr2=chr3='';enc1=enc2=enc3=enc4=''}while(i<input.length);return output};function getEncryptCode(key,value){return encode32(bin216(Base32.encrypt(value, key)));}";
            ScriptEngine script = new ScriptEngine(ScriptLanguage.JavaScript);
            var str = script.Run("getEncryptCode", new object[] { key, value }, func).ToString();
            return str.ToString();
        }
    }
}
