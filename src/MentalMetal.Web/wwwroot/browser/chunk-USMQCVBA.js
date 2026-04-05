import{$ as z,E as ie,J as re,L as se,M as rt,O as le,Q as yt,R as vt,S as K,T as ae,U as de,_ as ue,c as Et,ca as Nt,d as Y,da as ce,e as Jt,ea as U,f as te,fa as J,ga as S,ha as pe,l as ee,v as Bt,x as ne,y as oe,z as Dt}from"./chunk-4WP7HCMD.js";import{d as Xt,e as Yt,f as Kt,g as W,h as it,i as Tt}from"./chunk-APDUMELK.js";import{$ as Vt,$a as ht,Aa as bt,Ab as Pt,Ba as wt,Fb as O,Ga as B,Ha as M,Ia as D,J as k,Ja as Q,Jb as m,K as $,Ka as C,Kb as Qt,La as R,M as A,O as p,Ob as v,Pb as Zt,Ra as N,Ua as g,V as dt,Va as St,W as zt,Wa as xt,X as ut,Xa as et,Ya as kt,Za as _t,_ as ct,_a as It,a as c,ab as gt,b as at,bb as jt,c as Ot,da as tt,db as Rt,eb as Wt,fb as E,ga as F,gb as Z,ha as Ht,hb as X,ia as h,ib as Ut,ka as q,kb as nt,lb as ot,nb as Gt,oa as pt,pb as qt,qb as w,rb as ft,sb as mt,ta as y,xb as _}from"./chunk-CG5DNZS2.js";function V(...o){if(o){let l=[];for(let t=0;t<o.length;t++){let e=o[t];if(!e)continue;let n=typeof e;if(n==="string"||n==="number")l.push(e);else if(n==="object"){let i=Array.isArray(e)?[V(...e)]:Object.entries(e).map(([r,s])=>s?r:void 0);l=i.length?l.concat(i.filter(r=>!!r)):l}}return l.join(" ").trim()}}var ze=Object.defineProperty,be=Object.getOwnPropertySymbols,Ve=Object.prototype.hasOwnProperty,He=Object.prototype.propertyIsEnumerable,he=(o,l,t)=>l in o?ze(o,l,{enumerable:!0,configurable:!0,writable:!0,value:t}):o[l]=t,ge=(o,l)=>{for(var t in l||(l={}))Ve.call(l,t)&&he(o,t,l[t]);if(be)for(var t of be(l))He.call(l,t)&&he(o,t,l[t]);return o};function fe(...o){if(o){let l=[];for(let t=0;t<o.length;t++){let e=o[t];if(!e)continue;let n=typeof e;if(n==="string"||n==="number")l.push(e);else if(n==="object"){let i=Array.isArray(e)?[fe(...e)]:Object.entries(e).map(([r,s])=>s?r:void 0);l=i.length?l.concat(i.filter(r=>!!r)):l}}return l.join(" ").trim()}}function je(o){return typeof o=="function"&&"call"in o&&"apply"in o}function Re({skipUndefined:o=!1},...l){return l?.reduce((t,e={})=>{for(let n in e){let i=e[n];if(!(o&&i===void 0))if(n==="style")t.style=ge(ge({},t.style),e.style);else if(n==="class"||n==="className")t[n]=fe(t[n],e[n]);else if(je(i)){let r=t[n];t[n]=r?(...s)=>{r(...s),i(...s)}:i}else t[n]=i}return t},{})}function $t(...o){return Re({skipUndefined:!1},...o)}var Ct={};function st(o="pui_id_"){return Object.hasOwn(Ct,o)||(Ct[o]=0),Ct[o]++,`${o}${Ct[o]}`}var me=(()=>{class o extends S{name="common";static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275prov=k({token:o,factory:o.\u0275fac,providedIn:"root"})}return o})(),G=new A("PARENT_INSTANCE"),I=(()=>{class o{document=p(ut);platformId=p(pt);el=p(q);injector=p(zt);cd=p(Qt);renderer=p(bt);config=p(pe);$parentInstance=p(G,{optional:!0,skipSelf:!0})??void 0;baseComponentStyle=p(me);baseStyle=p(S);scopedStyleEl;parent=this.$params.parent;cn=V;_themeScopedListener;themeChangeListenerMap=new Map;dt=m();unstyled=m();pt=m();ptOptions=m();$attrSelector=st("pc");get $name(){return this.componentName||"UnknownComponent"}get $hostName(){return this.hostName}get $el(){return this.el?.nativeElement}directivePT=tt(void 0);directiveUnstyled=tt(void 0);$unstyled=O(()=>this.unstyled()??this.directiveUnstyled()??this.config?.unstyled()??!1);$pt=O(()=>yt(this.pt()||this.directivePT(),this.$params));get $globalPT(){return this._getPT(this.config?.pt(),void 0,t=>yt(t,this.$params))}get $defaultPT(){return this._getPT(this.config?.pt(),void 0,t=>this._getOptionValue(t,this.$hostName||this.$name,this.$params)||yt(t,this.$params))}get $style(){return c(c({theme:void 0,css:void 0,classes:void 0,inlineStyles:void 0},(this._getHostInstance(this)||{}).$style),this._componentStyle)}get $styleOptions(){return{nonce:this.config?.csp().nonce}}get $params(){let t=this._getHostInstance(this)||this.$parentInstance;return{instance:this,parent:{instance:t}}}onInit(){}onChanges(t){}onDoCheck(){}onAfterContentInit(){}onAfterContentChecked(){}onAfterViewInit(){}onAfterViewChecked(){}onDestroy(){}constructor(){F(t=>{this.document&&!Tt(this.platformId)&&(this.dt()?(this._loadScopedThemeStyles(this.dt()),this._themeScopedListener=()=>this._loadScopedThemeStyles(this.dt()),this._themeChangeListener("_themeScopedListener",this._themeScopedListener)):this._unloadScopedThemeStyles()),t(()=>{this._offThemeChangeListener("_themeScopedListener")})}),F(t=>{this.document&&!Tt(this.platformId)&&(this.$unstyled()||(this._loadCoreStyles(),this._themeChangeListener("_loadCoreStyles",this._loadCoreStyles))),t(()=>{this._offThemeChangeListener("_loadCoreStyles")})}),this._hook("onBeforeInit")}ngOnInit(){this._loadCoreStyles(),this._loadStyles(),this.onInit(),this._hook("onInit")}ngOnChanges(t){this.onChanges(t),this._hook("onChanges",t)}ngDoCheck(){this.onDoCheck(),this._hook("onDoCheck")}ngAfterContentInit(){this.onAfterContentInit(),this._hook("onAfterContentInit")}ngAfterContentChecked(){this.onAfterContentChecked(),this._hook("onAfterContentChecked")}ngAfterViewInit(){this.$el?.setAttribute(this.$attrSelector,""),this.onAfterViewInit(),this._hook("onAfterViewInit")}ngAfterViewChecked(){this.onAfterViewChecked(),this._hook("onAfterViewChecked")}ngOnDestroy(){this._removeThemeListeners(),this._unloadScopedThemeStyles(),this.onDestroy(),this._hook("onDestroy")}_mergeProps(t,...e){return se(t)?t(...e):$t(...e)}_getHostInstance(t){return t?this.$hostName?this.$name===this.$hostName?t:this._getHostInstance(t.$parentInstance):t.$parentInstance:void 0}_getPropValue(t){return this[t]||this._getHostInstance(this)?.[t]}_getOptionValue(t,e="",n={}){return ae(t,e,n)}_hook(t,...e){if(!this.$hostName){let n=this._usePT(this._getPT(this.$pt(),this.$name),this._getOptionValue,`hooks.${t}`),i=this._useDefaultPT(this._getOptionValue,`hooks.${t}`);n?.(...e),i?.(...e)}}_load(){J.isStyleNameLoaded("base")||(this.baseStyle.loadBaseCSS(this.$styleOptions),this._loadGlobalStyles(),J.setLoadedStyleName("base")),this._loadThemeStyles()}_loadStyles(){this._load(),this._themeChangeListener("_load",()=>this._load())}_loadGlobalStyles(){let t=this._useGlobalPT(this._getOptionValue,"global.css",this.$params);rt(t)&&this.baseStyle.load(t,c({name:"global"},this.$styleOptions))}_loadCoreStyles(){!J.isStyleNameLoaded(this.$style?.name)&&this.$style?.name&&(this.baseComponentStyle.loadCSS(this.$styleOptions),this.$style.loadCSS(this.$styleOptions),J.setLoadedStyleName(this.$style.name))}_loadThemeStyles(){if(!(this.$unstyled()||this.config?.theme()==="none")){if(!U.isStyleNameLoaded("common")){let{primitive:t,semantic:e,global:n,style:i}=this.$style?.getCommonTheme?.()||{};this.baseStyle.load(t?.css,c({name:"primitive-variables"},this.$styleOptions)),this.baseStyle.load(e?.css,c({name:"semantic-variables"},this.$styleOptions)),this.baseStyle.load(n?.css,c({name:"global-variables"},this.$styleOptions)),this.baseStyle.loadBaseStyle(c({name:"global-style"},this.$styleOptions),i),U.setLoadedStyleName("common")}if(!U.isStyleNameLoaded(this.$style?.name)&&this.$style?.name){let{css:t,style:e}=this.$style?.getComponentTheme?.()||{};this.$style?.load(t,c({name:`${this.$style?.name}-variables`},this.$styleOptions)),this.$style?.loadStyle(c({name:`${this.$style?.name}-style`},this.$styleOptions),e),U.setLoadedStyleName(this.$style?.name)}if(!U.isStyleNameLoaded("layer-order")){let t=this.$style?.getLayerOrderThemeCSS?.();this.baseStyle.load(t,c({name:"layer-order",first:!0},this.$styleOptions)),U.setLoadedStyleName("layer-order")}}}_loadScopedThemeStyles(t){let{css:e}=this.$style?.getPresetTheme?.(t,`[${this.$attrSelector}]`)||{},n=this.$style?.load(e,c({name:`${this.$attrSelector}-${this.$style?.name}`},this.$styleOptions));this.scopedStyleEl=n?.el}_unloadScopedThemeStyles(){this.scopedStyleEl?.remove()}_themeChangeListener(t,e=()=>{}){this._offThemeChangeListener(t),J.clearLoadedStyleNames();let n=e.bind(this);this.themeChangeListenerMap.set(t,n),Nt.on("theme:change",n)}_removeThemeListeners(){this._offThemeChangeListener("_themeScopedListener"),this._offThemeChangeListener("_loadCoreStyles"),this._offThemeChangeListener("_load")}_offThemeChangeListener(t){this.themeChangeListenerMap.has(t)&&(Nt.off("theme:change",this.themeChangeListenerMap.get(t)),this.themeChangeListenerMap.delete(t))}_getPTValue(t={},e="",n={},i=!0){let r=/./g.test(e)&&!!n[e.split(".")[0]],{mergeSections:s=!0,mergeProps:a=!1}=this._getPropValue("ptOptions")?.()||this.config?.ptOptions?.()||{},u=i?r?this._useGlobalPT(this._getPTClassValue,e,n):this._useDefaultPT(this._getPTClassValue,e,n):void 0,d=r?void 0:this._usePT(this._getPT(t,this.$hostName||this.$name),this._getPTClassValue,e,at(c({},n),{global:u||{}})),b=this._getPTDatasets(e);return s||!s&&d?a?this._mergeProps(a,u,d,b):c(c(c({},u),d),b):c(c({},d),b)}_getPTDatasets(t=""){let e="data-pc-",n=t==="root"&&rt(this.$pt()?.["data-pc-section"]);return t!=="transition"&&at(c({},t==="root"&&at(c({[`${e}name`]:K(n?this.$pt()?.["data-pc-section"]:this.$name)},n&&{[`${e}extend`]:K(this.$name)}),{[`${this.$attrSelector}`]:""})),{[`${e}section`]:K(t.includes(".")?t.split(".").at(-1)??"":t)})}_getPTClassValue(t,e,n){let i=this._getOptionValue(t,e,n);return vt(i)||de(i)?{class:i}:i}_getPT(t,e="",n){let i=(r,s=!1)=>{let a=n?n(r):r,u=K(e),d=K(this.$hostName||this.$name);return(s?u!==d?a?.[u]:void 0:a?.[u])??a};return t?.hasOwnProperty("_usept")?{_usept:t._usept,originalValue:i(t.originalValue),value:i(t.value)}:i(t,!0)}_usePT(t,e,n,i){let r=s=>e?.call(this,s,n,i);if(t?.hasOwnProperty("_usept")){let{mergeSections:s=!0,mergeProps:a=!1}=t._usept||this.config?.ptOptions()||{},u=r(t.originalValue),d=r(t.value);return u===void 0&&d===void 0?void 0:vt(d)?d:vt(u)?u:s||!s&&d?a?this._mergeProps(a,u,d):c(c({},u),d):d}return r(t)}_useGlobalPT(t,e,n){return this._usePT(this.$globalPT,t,e,n)}_useDefaultPT(t,e,n){return this._usePT(this.$defaultPT,t,e,n)}ptm(t="",e={}){return this._getPTValue(this.$pt(),t,c(c({},this.$params),e))}ptms(t,e={}){return t.reduce((n,i)=>(n=$t(n,this.ptm(i,e))||{},n),{})}ptmo(t={},e="",n={}){return this._getPTValue(t,e,c({instance:this},n),!1)}cx(t,e={}){return this.$unstyled()?void 0:V(this._getOptionValue(this.$style.classes,t,c(c({},this.$params),e)))}sx(t="",e=!0,n={}){if(e){let i=this._getOptionValue(this.$style.inlineStyles,t,c(c({},this.$params),n)),r=this._getOptionValue(this.baseComponentStyle.inlineStyles,t,c(c({},this.$params),n));return c(c({},r),i)}}static \u0275fac=function(e){return new(e||o)};static \u0275dir=D({type:o,inputs:{dt:[1,"dt"],unstyled:[1,"unstyled"],pt:[1,"pt"],ptOptions:[1,"ptOptions"]},features:[_([me,S]),Ht]})}return o})();var Ft=(()=>{class o{static zindex=1e3;static calculatedScrollbarWidth=null;static calculatedScrollbarHeight=null;static browser;static addClass(t,e){t&&e&&(t.classList?t.classList.add(e):t.className+=" "+e)}static addMultipleClasses(t,e){if(t&&e)if(t.classList){let n=e.trim().split(" ");for(let i=0;i<n.length;i++)t.classList.add(n[i])}else{let n=e.split(" ");for(let i=0;i<n.length;i++)t.className+=" "+n[i]}}static removeClass(t,e){t&&e&&(t.classList?t.classList.remove(e):t.className=t.className.replace(new RegExp("(^|\\b)"+e.split(" ").join("|")+"(\\b|$)","gi")," "))}static removeMultipleClasses(t,e){t&&e&&[e].flat().filter(Boolean).forEach(n=>n.split(" ").forEach(i=>this.removeClass(t,i)))}static hasClass(t,e){return t&&e?t.classList?t.classList.contains(e):new RegExp("(^| )"+e+"( |$)","gi").test(t.className):!1}static siblings(t){return Array.prototype.filter.call(t.parentNode.children,function(e){return e!==t})}static find(t,e){return Array.from(t.querySelectorAll(e))}static findSingle(t,e){return this.isElement(t)?t.querySelector(e):null}static index(t){let e=t.parentNode.childNodes,n=0;for(var i=0;i<e.length;i++){if(e[i]==t)return n;e[i].nodeType==1&&n++}return-1}static indexWithinGroup(t,e){let n=t.parentNode?t.parentNode.childNodes:[],i=0;for(var r=0;r<n.length;r++){if(n[r]==t)return i;n[r].attributes&&n[r].attributes[e]&&n[r].nodeType==1&&i++}return-1}static appendOverlay(t,e,n="self"){n!=="self"&&t&&e&&this.appendChild(t,e)}static alignOverlay(t,e,n="self",i=!0){t&&e&&(i&&(t.style.minWidth=`${o.getOuterWidth(e)}px`),n==="self"?this.relativePosition(t,e):this.absolutePosition(t,e))}static relativePosition(t,e,n=!0){let i=j=>{if(j)return getComputedStyle(j).getPropertyValue("position")==="relative"?j:i(j.parentElement)},r=t.offsetParent?{width:t.offsetWidth,height:t.offsetHeight}:this.getHiddenElementDimensions(t),s=e.offsetHeight,a=e.getBoundingClientRect(),u=this.getWindowScrollTop(),d=this.getWindowScrollLeft(),b=this.getViewport(),f=i(t)?.getBoundingClientRect()||{top:-1*u,left:-1*d},T,L,lt="top";a.top+s+r.height>b.height?(T=a.top-f.top-r.height,lt="bottom",a.top+T<0&&(T=-1*a.top)):(T=s+a.top-f.top,lt="top");let At=a.left+r.width-b.width,Oe=a.left-f.left;if(r.width>b.width?L=(a.left-f.left)*-1:At>0?L=Oe-At:L=a.left-f.left,t.style.top=T+"px",t.style.left=L+"px",t.style.transformOrigin=lt,n){let j=te(/-anchor-gutter$/)?.value;t.style.marginTop=lt==="bottom"?`calc(${j??"2px"} * -1)`:j??""}}static absolutePosition(t,e,n=!0){let i=t.offsetParent?{width:t.offsetWidth,height:t.offsetHeight}:this.getHiddenElementDimensions(t),r=i.height,s=i.width,a=e.offsetHeight,u=e.offsetWidth,d=e.getBoundingClientRect(),b=this.getWindowScrollTop(),P=this.getWindowScrollLeft(),f=this.getViewport(),T,L;d.top+a+r>f.height?(T=d.top+b-r,t.style.transformOrigin="bottom",T<0&&(T=b)):(T=a+d.top+b,t.style.transformOrigin="top"),d.left+s>f.width?L=Math.max(0,d.left+P+u-s):L=d.left+P,t.style.top=T+"px",t.style.left=L+"px",n&&(t.style.marginTop=origin==="bottom"?"calc(var(--p-anchor-gutter) * -1)":"calc(var(--p-anchor-gutter))")}static getParents(t,e=[]){return t.parentNode===null?e:this.getParents(t.parentNode,e.concat([t.parentNode]))}static getScrollableParents(t){let e=[];if(t){let n=this.getParents(t),i=/(auto|scroll)/,r=s=>{let a=window.getComputedStyle(s,null);return i.test(a.getPropertyValue("overflow"))||i.test(a.getPropertyValue("overflowX"))||i.test(a.getPropertyValue("overflowY"))};for(let s of n){let a=s.nodeType===1&&s.dataset.scrollselectors;if(a){let u=a.split(",");for(let d of u){let b=this.findSingle(s,d);b&&r(b)&&e.push(b)}}s.nodeType!==9&&r(s)&&e.push(s)}}return e}static getHiddenElementOuterHeight(t){t.style.visibility="hidden",t.style.display="block";let e=t.offsetHeight;return t.style.display="none",t.style.visibility="visible",e}static getHiddenElementOuterWidth(t){t.style.visibility="hidden",t.style.display="block";let e=t.offsetWidth;return t.style.display="none",t.style.visibility="visible",e}static getHiddenElementDimensions(t){let e={};return t.style.visibility="hidden",t.style.display="block",e.width=t.offsetWidth,e.height=t.offsetHeight,t.style.display="none",t.style.visibility="visible",e}static scrollInView(t,e){let n=getComputedStyle(t).getPropertyValue("borderTopWidth"),i=n?parseFloat(n):0,r=getComputedStyle(t).getPropertyValue("paddingTop"),s=r?parseFloat(r):0,a=t.getBoundingClientRect(),d=e.getBoundingClientRect().top+document.body.scrollTop-(a.top+document.body.scrollTop)-i-s,b=t.scrollTop,P=t.clientHeight,f=this.getOuterHeight(e);d<0?t.scrollTop=b+d:d+f>P&&(t.scrollTop=b+d-P+f)}static fadeIn(t,e){t.style.opacity=0;let n=+new Date,i=0,r=function(){i=+t.style.opacity.replace(",",".")+(new Date().getTime()-n)/e,t.style.opacity=i,n=+new Date,+i<1&&(window.requestAnimationFrame?window.requestAnimationFrame(r):setTimeout(r,16))};r()}static fadeOut(t,e){var n=1,i=50,r=e,s=i/r;let a=setInterval(()=>{n=n-s,n<=0&&(n=0,clearInterval(a)),t.style.opacity=n},i)}static getWindowScrollTop(){let t=document.documentElement;return(window.pageYOffset||t.scrollTop)-(t.clientTop||0)}static getWindowScrollLeft(){let t=document.documentElement;return(window.pageXOffset||t.scrollLeft)-(t.clientLeft||0)}static matches(t,e){var n=Element.prototype,i=n.matches||n.webkitMatchesSelector||n.mozMatchesSelector||n.msMatchesSelector||function(r){return[].indexOf.call(document.querySelectorAll(r),this)!==-1};return i.call(t,e)}static getOuterWidth(t,e){let n=t.offsetWidth;if(e){let i=getComputedStyle(t);n+=parseFloat(i.marginLeft)+parseFloat(i.marginRight)}return n}static getHorizontalPadding(t){let e=getComputedStyle(t);return parseFloat(e.paddingLeft)+parseFloat(e.paddingRight)}static getHorizontalMargin(t){let e=getComputedStyle(t);return parseFloat(e.marginLeft)+parseFloat(e.marginRight)}static innerWidth(t){let e=t.offsetWidth,n=getComputedStyle(t);return e+=parseFloat(n.paddingLeft)+parseFloat(n.paddingRight),e}static width(t){let e=t.offsetWidth,n=getComputedStyle(t);return e-=parseFloat(n.paddingLeft)+parseFloat(n.paddingRight),e}static getInnerHeight(t){let e=t.offsetHeight,n=getComputedStyle(t);return e+=parseFloat(n.paddingTop)+parseFloat(n.paddingBottom),e}static getOuterHeight(t,e){let n=t.offsetHeight;if(e){let i=getComputedStyle(t);n+=parseFloat(i.marginTop)+parseFloat(i.marginBottom)}return n}static getHeight(t){let e=t.offsetHeight,n=getComputedStyle(t);return e-=parseFloat(n.paddingTop)+parseFloat(n.paddingBottom)+parseFloat(n.borderTopWidth)+parseFloat(n.borderBottomWidth),e}static getWidth(t){let e=t.offsetWidth,n=getComputedStyle(t);return e-=parseFloat(n.paddingLeft)+parseFloat(n.paddingRight)+parseFloat(n.borderLeftWidth)+parseFloat(n.borderRightWidth),e}static getViewport(){let t=window,e=document,n=e.documentElement,i=e.getElementsByTagName("body")[0],r=t.innerWidth||n.clientWidth||i.clientWidth,s=t.innerHeight||n.clientHeight||i.clientHeight;return{width:r,height:s}}static getOffset(t){var e=t.getBoundingClientRect();return{top:e.top+(window.pageYOffset||document.documentElement.scrollTop||document.body.scrollTop||0),left:e.left+(window.pageXOffset||document.documentElement.scrollLeft||document.body.scrollLeft||0)}}static replaceElementWith(t,e){let n=t.parentNode;if(!n)throw"Can't replace element";return n.replaceChild(e,t)}static getUserAgent(){if(navigator&&this.isClient())return navigator.userAgent}static isIE(){var t=window.navigator.userAgent,e=t.indexOf("MSIE ");if(e>0)return!0;var n=t.indexOf("Trident/");if(n>0){var i=t.indexOf("rv:");return!0}var r=t.indexOf("Edge/");return r>0}static isIOS(){return/iPad|iPhone|iPod/.test(navigator.userAgent)&&!window.MSStream}static isAndroid(){return/(android)/i.test(navigator.userAgent)}static isTouchDevice(){return"ontouchstart"in window||navigator.maxTouchPoints>0}static appendChild(t,e){if(this.isElement(e))e.appendChild(t);else if(e&&e.el&&e.el.nativeElement)e.el.nativeElement.appendChild(t);else throw"Cannot append "+e+" to "+t}static removeChild(t,e){if(this.isElement(e))e.removeChild(t);else if(e.el&&e.el.nativeElement)e.el.nativeElement.removeChild(t);else throw"Cannot remove "+t+" from "+e}static removeElement(t){"remove"in Element.prototype?t.remove():t.parentNode?.removeChild(t)}static isElement(t){return typeof HTMLElement=="object"?t instanceof HTMLElement:t&&typeof t=="object"&&t!==null&&t.nodeType===1&&typeof t.nodeName=="string"}static calculateScrollbarWidth(t){if(t){let e=getComputedStyle(t);return t.offsetWidth-t.clientWidth-parseFloat(e.borderLeftWidth)-parseFloat(e.borderRightWidth)}else{if(this.calculatedScrollbarWidth!==null)return this.calculatedScrollbarWidth;let e=document.createElement("div");e.className="p-scrollbar-measure",document.body.appendChild(e);let n=e.offsetWidth-e.clientWidth;return document.body.removeChild(e),this.calculatedScrollbarWidth=n,n}}static calculateScrollbarHeight(){if(this.calculatedScrollbarHeight!==null)return this.calculatedScrollbarHeight;let t=document.createElement("div");t.className="p-scrollbar-measure",document.body.appendChild(t);let e=t.offsetHeight-t.clientHeight;return document.body.removeChild(t),this.calculatedScrollbarWidth=e,e}static invokeElementMethod(t,e,n){t[e].apply(t,n)}static clearSelection(){if(window.getSelection&&window.getSelection())window.getSelection()?.empty?window.getSelection()?.empty():window.getSelection()?.removeAllRanges&&(window.getSelection()?.rangeCount||0)>0&&(window.getSelection()?.getRangeAt(0)?.getClientRects()?.length||0)>0&&window.getSelection()?.removeAllRanges();else if(document.selection&&document.selection.empty)try{document.selection.empty()}catch{}}static getBrowser(){if(!this.browser){let t=this.resolveUserAgent();this.browser={},t.browser&&(this.browser[t.browser]=!0,this.browser.version=t.version),this.browser.chrome?this.browser.webkit=!0:this.browser.webkit&&(this.browser.safari=!0)}return this.browser}static resolveUserAgent(){let t=navigator.userAgent.toLowerCase(),e=/(chrome)[ \/]([\w.]+)/.exec(t)||/(webkit)[ \/]([\w.]+)/.exec(t)||/(opera)(?:.*version|)[ \/]([\w.]+)/.exec(t)||/(msie) ([\w.]+)/.exec(t)||t.indexOf("compatible")<0&&/(mozilla)(?:.*? rv:([\w.]+)|)/.exec(t)||[];return{browser:e[1]||"",version:e[2]||"0"}}static isInteger(t){return Number.isInteger?Number.isInteger(t):typeof t=="number"&&isFinite(t)&&Math.floor(t)===t}static isHidden(t){return!t||t.offsetParent===null}static isVisible(t){return t&&t.offsetParent!=null}static isExist(t){return t!==null&&typeof t<"u"&&t.nodeName&&t.parentNode}static focus(t,e){t&&document.activeElement!==t&&t.focus(e)}static getFocusableSelectorString(t=""){return`button:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        [href][clientHeight][clientWidth]:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        input:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        select:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        textarea:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        [tabIndex]:not([tabIndex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        [contenteditable]:not([tabIndex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        .p-inputtext:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t},
        .p-button:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${t}`}static getFocusableElements(t,e=""){let n=this.find(t,this.getFocusableSelectorString(e)),i=[];for(let r of n){let s=getComputedStyle(r);this.isVisible(r)&&s.display!="none"&&s.visibility!="hidden"&&i.push(r)}return i}static getFocusableElement(t,e=""){let n=this.findSingle(t,this.getFocusableSelectorString(e));if(n){let i=getComputedStyle(n);if(this.isVisible(n)&&i.display!="none"&&i.visibility!="hidden")return n}return null}static getFirstFocusableElement(t,e=""){let n=this.getFocusableElements(t,e);return n.length>0?n[0]:null}static getLastFocusableElement(t,e){let n=this.getFocusableElements(t,e);return n.length>0?n[n.length-1]:null}static getNextFocusableElement(t,e=!1){let n=o.getFocusableElements(t),i=0;if(n&&n.length>0){let r=n.indexOf(n[0].ownerDocument.activeElement);e?r==-1||r===0?i=n.length-1:i=r-1:r!=-1&&r!==n.length-1&&(i=r+1)}return n[i]}static generateZIndex(){return this.zindex=this.zindex||999,++this.zindex}static getSelection(){return window.getSelection?window.getSelection()?.toString():document.getSelection?document.getSelection()?.toString():document.selection?document.selection.createRange().text:null}static getTargetElement(t,e){if(!t)return null;switch(t){case"document":return document;case"window":return window;case"@next":return e?.nextElementSibling;case"@prev":return e?.previousElementSibling;case"@parent":return e?.parentElement;case"@grandparent":return e?.parentElement?.parentElement;default:let n=typeof t;if(n==="string")return document.querySelector(t);if(n==="object"&&t.hasOwnProperty("nativeElement"))return this.isExist(t.nativeElement)?t.nativeElement:void 0;let r=(s=>!!(s&&s.constructor&&s.call&&s.apply))(t)?t():t;return r&&r.nodeType===9||this.isExist(r)?r:null}}static isClient(){return!!(typeof window<"u"&&window.document&&window.document.createElement)}static getAttribute(t,e){if(t){let n=t.getAttribute(e);return isNaN(n)?n==="true"||n==="false"?n==="true":n:+n}}static calculateBodyScrollbarWidth(){return window.innerWidth-document.documentElement.offsetWidth}static blockBodyScroll(t="p-overflow-hidden"){document.body.style.setProperty("--scrollbar-width",this.calculateBodyScrollbarWidth()+"px"),this.addClass(document.body,t)}static unblockBodyScroll(t="p-overflow-hidden"){document.body.style.removeProperty("--scrollbar-width"),this.removeClass(document.body,t)}static createElement(t,e={},...n){if(t){let i=document.createElement(t);return this.setAttributes(i,e),i.append(...n),i}}static setAttribute(t,e="",n){this.isElement(t)&&n!==null&&n!==void 0&&t.setAttribute(e,n)}static setAttributes(t,e={}){if(this.isElement(t)){let n=(i,r)=>{let s=t?.$attrs?.[i]?[t?.$attrs?.[i]]:[];return[r].flat().reduce((a,u)=>{if(u!=null){let d=typeof u;if(d==="string"||d==="number")a.push(u);else if(d==="object"){let b=Array.isArray(u)?n(i,u):Object.entries(u).map(([P,f])=>i==="style"&&(f||f===0)?`${P.replace(/([a-z])([A-Z])/g,"$1-$2").toLowerCase()}:${f}`:f?P:void 0);a=b.length?a.concat(b.filter(P=>!!P)):a}}return a},s)};Object.entries(e).forEach(([i,r])=>{if(r!=null){let s=i.match(/^on(.+)/);s?t.addEventListener(s[1].toLowerCase(),r):i==="pBind"?this.setAttributes(t,r):(r=i==="class"?[...new Set(n("class",r))].join(" ").trim():i==="style"?n("style",r).join(";").trim():r,(t.$attrs=t.$attrs||{})&&(t.$attrs[i]=r),t.setAttribute(i,r))}})}}static isFocusableElement(t,e=""){return this.isElement(t)?t.matches(`button:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${e},
                [href][clientHeight][clientWidth]:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${e},
                input:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${e},
                select:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${e},
                textarea:not([tabindex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${e},
                [tabIndex]:not([tabIndex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${e},
                [contenteditable]:not([tabIndex = "-1"]):not([disabled]):not([style*="display:none"]):not([hidden])${e}`):!1}}return o})();function An(){Jt({variableName:ce("scrollbar.width").name})}var ye=class{element;listener;scrollableParents;constructor(l,t=()=>{}){this.element=l,this.listener=t}bindScrollListener(){this.scrollableParents=Ft.getScrollableParents(this.element);for(let l=0;l<this.scrollableParents.length;l++)this.scrollableParents[l].addEventListener("scroll",this.listener)}unbindScrollListener(){if(this.scrollableParents)for(let l=0;l<this.scrollableParents.length;l++)this.scrollableParents[l].removeEventListener("scroll",this.listener)}destroy(){this.unbindScrollListener(),this.element=null,this.listener=null,this.scrollableParents=null}};var ve=(()=>{class o extends I{autofocus=!1;focused=!1;platformId=p(pt);document=p(ut);host=p(q);onAfterContentChecked(){this.autofocus===!1?this.host.nativeElement.removeAttribute("autofocus"):this.host.nativeElement.setAttribute("autofocus",!0),this.focused||this.autoFocus()}onAfterViewChecked(){this.focused||this.autoFocus()}autoFocus(){it(this.platformId)&&this.autofocus&&setTimeout(()=>{let t=Ft.getFocusableElements(this.host?.nativeElement);t.length===0&&this.host.nativeElement.focus(),t.length>0&&t[0].focus(),this.focused=!0})}static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275dir=D({type:o,selectors:[["","pAutoFocus",""]],inputs:{autofocus:[0,"pAutoFocus","autofocus"]},features:[C]})}return o})();var x=(()=>{class o{el;renderer;pBind=m(void 0);_attrs=tt(void 0);attrs=O(()=>this._attrs()||this.pBind());styles=O(()=>this.attrs()?.style);classes=O(()=>V(this.attrs()?.class));listeners=[];constructor(t,e){this.el=t,this.renderer=e,F(()=>{let s=this.attrs()||{},{style:n,class:i}=s,r=Ot(s,["style","class"]);for(let[a,u]of Object.entries(r))if(a.startsWith("on")&&typeof u=="function"){let d=a.slice(2).toLowerCase();if(!this.listeners.some(b=>b.eventName===d)){let b=this.renderer.listen(this.el.nativeElement,d,u);this.listeners.push({eventName:d,unlisten:b})}}else u==null?this.renderer.removeAttribute(this.el.nativeElement,a):(this.renderer.setAttribute(this.el.nativeElement,a,u.toString()),a in this.el.nativeElement&&(this.el.nativeElement[a]=u))})}ngOnDestroy(){this.clearListeners()}setAttrs(t){le(this._attrs(),t)||this._attrs.set(t)}clearListeners(){this.listeners.forEach(({unlisten:t})=>t()),this.listeners=[]}static \u0275fac=function(e){return new(e||o)(wt(q),wt(bt))};static \u0275dir=D({type:o,selectors:[["","pBind",""]],hostVars:4,hostBindings:function(e,n){e&2&&(qt(n.styles()),w(n.classes()))},inputs:{pBind:[1,"pBind"]}})}return o})(),Ce=(()=>{class o{static \u0275fac=function(e){return new(e||o)};static \u0275mod=M({type:o});static \u0275inj=$({})}return o})();var we=`
    .p-badge {
        display: inline-flex;
        border-radius: dt('badge.border.radius');
        align-items: center;
        justify-content: center;
        padding: dt('badge.padding');
        background: dt('badge.primary.background');
        color: dt('badge.primary.color');
        font-size: dt('badge.font.size');
        font-weight: dt('badge.font.weight');
        min-width: dt('badge.min.width');
        height: dt('badge.height');
    }

    .p-badge-dot {
        width: dt('badge.dot.size');
        min-width: dt('badge.dot.size');
        height: dt('badge.dot.size');
        border-radius: 50%;
        padding: 0;
    }

    .p-badge-circle {
        padding: 0;
        border-radius: 50%;
    }

    .p-badge-secondary {
        background: dt('badge.secondary.background');
        color: dt('badge.secondary.color');
    }

    .p-badge-success {
        background: dt('badge.success.background');
        color: dt('badge.success.color');
    }

    .p-badge-info {
        background: dt('badge.info.background');
        color: dt('badge.info.color');
    }

    .p-badge-warn {
        background: dt('badge.warn.background');
        color: dt('badge.warn.color');
    }

    .p-badge-danger {
        background: dt('badge.danger.background');
        color: dt('badge.danger.color');
    }

    .p-badge-contrast {
        background: dt('badge.contrast.background');
        color: dt('badge.contrast.color');
    }

    .p-badge-sm {
        font-size: dt('badge.sm.font.size');
        min-width: dt('badge.sm.min.width');
        height: dt('badge.sm.height');
    }

    .p-badge-lg {
        font-size: dt('badge.lg.font.size');
        min-width: dt('badge.lg.min.width');
        height: dt('badge.lg.height');
    }

    .p-badge-xl {
        font-size: dt('badge.xl.font.size');
        min-width: dt('badge.xl.min.width');
        height: dt('badge.xl.height');
    }
`;var We=`
    ${we}

    /* For PrimeNG (directive)*/
    .p-overlay-badge {
        position: relative;
    }

    .p-overlay-badge > .p-badge {
        position: absolute;
        top: 0;
        inset-inline-end: 0;
        transform: translate(50%, -50%);
        transform-origin: 100% 0;
        margin: 0;
    }
`,Ue={root:({instance:o})=>{let l=typeof o.value=="function"?o.value():o.value,t=typeof o.size=="function"?o.size():o.size,e=typeof o.badgeSize=="function"?o.badgeSize():o.badgeSize,n=typeof o.severity=="function"?o.severity():o.severity;return["p-badge p-component",{"p-badge-circle":rt(l)&&String(l).length===1,"p-badge-dot":re(l),"p-badge-sm":t==="small"||e==="small","p-badge-lg":t==="large"||e==="large","p-badge-xl":t==="xlarge"||e==="xlarge","p-badge-info":n==="info","p-badge-success":n==="success","p-badge-warn":n==="warn","p-badge-danger":n==="danger","p-badge-secondary":n==="secondary","p-badge-contrast":n==="contrast"}]}},Se=(()=>{class o extends S{name="badge";style=We;classes=Ue;static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275prov=k({token:o,factory:o.\u0275fac})}return o})();var xe=new A("BADGE_INSTANCE");var Lt=(()=>{class o extends I{componentName="Badge";$pcBadge=p(xe,{optional:!0,skipSelf:!0})??void 0;bindDirectiveInstance=p(x,{self:!0});onAfterViewChecked(){this.bindDirectiveInstance.setAttrs(this.ptms(["host","root"]))}styleClass=m();badgeSize=m();size=m();severity=m();value=m();badgeDisabled=m(!1,{transform:v});_componentStyle=p(Se);get dataP(){return this.cn({circle:this.value()!=null&&String(this.value()).length===1,empty:this.value()==null,disabled:this.badgeDisabled(),[this.severity()]:this.severity(),[this.size()]:this.size()})}static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275cmp=B({type:o,selectors:[["p-badge"]],hostVars:5,hostBindings:function(e,n){e&2&&(N("data-p",n.dataP),w(n.cn(n.cx("root"),n.styleClass())),Gt("display",n.badgeDisabled()?"none":null))},inputs:{styleClass:[1,"styleClass"],badgeSize:[1,"badgeSize"],size:[1,"size"],severity:[1,"severity"],value:[1,"value"],badgeDisabled:[1,"badgeDisabled"]},features:[_([Se,{provide:xe,useExisting:o},{provide:G,useExisting:o}]),Q([x]),C],decls:1,vars:1,template:function(e,n){e&1&&ft(0),e&2&&mt(n.value())},dependencies:[W,z,Ce],encapsulation:2,changeDetection:0})}return o})(),ke=(()=>{class o{static \u0275fac=function(e){return new(e||o)};static \u0275mod=M({type:o});static \u0275inj=$({imports:[Lt,z,z]})}return o})();var qe=["*"],Qe={root:"p-fluid"},_e=(()=>{class o extends S{name="fluid";classes=Qe;static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275prov=k({token:o,factory:o.\u0275fac})}return o})();var Ie=new A("FLUID_INSTANCE"),Pe=(()=>{class o extends I{componentName="Fluid";$pcFluid=p(Ie,{optional:!0,skipSelf:!0})??void 0;bindDirectiveInstance=p(x,{self:!0});onAfterViewChecked(){this.bindDirectiveInstance.setAttrs(this.ptms(["host","root"]))}_componentStyle=p(_e);static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275cmp=B({type:o,selectors:[["p-fluid"]],hostVars:2,hostBindings:function(e,n){e&2&&w(n.cx("root"))},features:[_([_e,{provide:Ie,useExisting:o},{provide:G,useExisting:o}]),Q([x]),C],ngContentSelectors:qe,decls:1,vars:0,template:function(e,n){e&1&&(Z(),X(0))},dependencies:[W],encapsulation:2,changeDetection:0})}return o})();var Ze=["*"],Xe=`
.p-icon {
    display: inline-block;
    vertical-align: baseline;
    flex-shrink: 0;
}

.p-icon-spin {
    -webkit-animation: p-icon-spin 2s infinite linear;
    animation: p-icon-spin 2s infinite linear;
}

@-webkit-keyframes p-icon-spin {
    0% {
        -webkit-transform: rotate(0deg);
        transform: rotate(0deg);
    }
    100% {
        -webkit-transform: rotate(359deg);
        transform: rotate(359deg);
    }
}

@keyframes p-icon-spin {
    0% {
        -webkit-transform: rotate(0deg);
        transform: rotate(0deg);
    }
    100% {
        -webkit-transform: rotate(359deg);
        transform: rotate(359deg);
    }
}
`,Te=(()=>{class o extends S{name="baseicon";css=Xe;static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275prov=k({token:o,factory:o.\u0275fac,providedIn:"root"})}return o})();var Ee=(()=>{class o extends I{spin=!1;_componentStyle=p(Te);getClassNames(){return V("p-icon",{"p-icon-spin":this.spin})}static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275cmp=B({type:o,selectors:[["ng-component"]],hostAttrs:["width","14","height","14","viewBox","0 0 14 14","fill","none","xmlns","http://www.w3.org/2000/svg"],hostVars:2,hostBindings:function(e,n){e&2&&w(n.getClassNames())},inputs:{spin:[2,"spin","spin",v]},features:[_([Te]),C],ngContentSelectors:Ze,decls:1,vars:0,template:function(e,n){e&1&&(Z(),X(0))},encapsulation:2,changeDetection:0})}return o})();var Ye=["data-p-icon","spinner"],Be=(()=>{class o extends Ee{pathId;onInit(){this.pathId="url(#"+st()+")"}static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275cmp=B({type:o,selectors:[["","data-p-icon","spinner"]],features:[C],attrs:Ye,decls:5,vars:2,consts:[["d","M6.99701 14C5.85441 13.999 4.72939 13.7186 3.72012 13.1832C2.71084 12.6478 1.84795 11.8737 1.20673 10.9284C0.565504 9.98305 0.165424 8.89526 0.041387 7.75989C-0.0826496 6.62453 0.073125 5.47607 0.495122 4.4147C0.917119 3.35333 1.59252 2.4113 2.46241 1.67077C3.33229 0.930247 4.37024 0.413729 5.4857 0.166275C6.60117 -0.0811796 7.76026 -0.0520535 8.86188 0.251112C9.9635 0.554278 10.9742 1.12227 11.8057 1.90555C11.915 2.01493 11.9764 2.16319 11.9764 2.31778C11.9764 2.47236 11.915 2.62062 11.8057 2.73C11.7521 2.78503 11.688 2.82877 11.6171 2.85864C11.5463 2.8885 11.4702 2.90389 11.3933 2.90389C11.3165 2.90389 11.2404 2.8885 11.1695 2.85864C11.0987 2.82877 11.0346 2.78503 10.9809 2.73C9.9998 1.81273 8.73246 1.26138 7.39226 1.16876C6.05206 1.07615 4.72086 1.44794 3.62279 2.22152C2.52471 2.99511 1.72683 4.12325 1.36345 5.41602C1.00008 6.70879 1.09342 8.08723 1.62775 9.31926C2.16209 10.5513 3.10478 11.5617 4.29713 12.1803C5.48947 12.7989 6.85865 12.988 8.17414 12.7157C9.48963 12.4435 10.6711 11.7264 11.5196 10.6854C12.3681 9.64432 12.8319 8.34282 12.8328 7C12.8328 6.84529 12.8943 6.69692 13.0038 6.58752C13.1132 6.47812 13.2616 6.41667 13.4164 6.41667C13.5712 6.41667 13.7196 6.47812 13.8291 6.58752C13.9385 6.69692 14 6.84529 14 7C14 8.85651 13.2622 10.637 11.9489 11.9497C10.6356 13.2625 8.85432 14 6.99701 14Z","fill","currentColor"],[3,"id"],["width","14","height","14","fill","white"]],template:function(e,n){e&1&&(dt(),kt(0,"g"),It(1,"path",0),_t(),kt(2,"defs")(3,"clipPath",1),It(4,"rect",2),_t()()),e&2&&(N("clip-path",n.pathId),y(3),Rt("id",n.pathId))},encapsulation:2})}return o})();var De=`
    .p-ink {
        display: block;
        position: absolute;
        background: dt('ripple.background');
        border-radius: 100%;
        transform: scale(0);
        pointer-events: none;
    }

    .p-ink-active {
        animation: ripple 0.4s linear;
    }

    @keyframes ripple {
        100% {
            opacity: 0;
            transform: scale(2.5);
        }
    }
`;var Ke=`
    ${De}

    /* For PrimeNG */
    .p-ripple {
        overflow: hidden;
        position: relative;
    }

    .p-ripple-disabled .p-ink {
        display: none !important;
    }

    @keyframes ripple {
        100% {
            opacity: 0;
            transform: scale(2.5);
        }
    }
`,Je={root:"p-ink"},Ne=(()=>{class o extends S{name="ripple";style=Ke;classes=Je;static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275prov=k({token:o,factory:o.\u0275fac})}return o})();var $e=(()=>{class o extends I{componentName="Ripple";zone=p(Vt);_componentStyle=p(Ne);animationListener;mouseDownListener;timeout;constructor(){super(),F(()=>{it(this.platformId)&&(this.config.ripple()?this.zone.runOutsideAngular(()=>{this.create(),this.mouseDownListener=this.renderer.listen(this.el.nativeElement,"mousedown",this.onMouseDown.bind(this))}):this.remove())})}onAfterViewInit(){}onMouseDown(t){let e=this.getInk();if(!e||this.document.defaultView?.getComputedStyle(e,null).display==="none")return;if(!this.$unstyled()&&Y(e,"p-ink-active"),e.setAttribute("data-p-ink-active","false"),!Bt(e)&&!Dt(e)){let s=Math.max(ee(this.el.nativeElement),oe(this.el.nativeElement));e.style.height=s+"px",e.style.width=s+"px"}let n=ne(this.el.nativeElement),i=t.pageX-n.left+this.document.body.scrollTop-Dt(e)/2,r=t.pageY-n.top+this.document.body.scrollLeft-Bt(e)/2;this.renderer.setStyle(e,"top",r+"px"),this.renderer.setStyle(e,"left",i+"px"),!this.$unstyled()&&Et(e,"p-ink-active"),e.setAttribute("data-p-ink-active","true"),this.timeout=setTimeout(()=>{let s=this.getInk();s&&(!this.$unstyled()&&Y(s,"p-ink-active"),s.setAttribute("data-p-ink-active","false"))},401)}getInk(){let t=this.el.nativeElement.children;for(let e=0;e<t.length;e++)if(typeof t[e].className=="string"&&t[e].className.indexOf("p-ink")!==-1)return t[e];return null}resetInk(){let t=this.getInk();t&&(!this.$unstyled()&&Y(t,"p-ink-active"),t.setAttribute("data-p-ink-active","false"))}onAnimationEnd(t){this.timeout&&clearTimeout(this.timeout),!this.$unstyled()&&Y(t.currentTarget,"p-ink-active"),t.currentTarget.setAttribute("data-p-ink-active","false")}create(){let t=this.renderer.createElement("span");this.renderer.addClass(t,"p-ink"),this.renderer.appendChild(this.el.nativeElement,t),this.renderer.setAttribute(t,"data-p-ink","true"),this.renderer.setAttribute(t,"data-p-ink-active","false"),this.renderer.setAttribute(t,"aria-hidden","true"),this.renderer.setAttribute(t,"role","presentation"),this.animationListener||(this.animationListener=this.renderer.listen(t,"animationend",this.onAnimationEnd.bind(this)))}remove(){let t=this.getInk();t&&(this.mouseDownListener&&this.mouseDownListener(),this.animationListener&&this.animationListener(),this.mouseDownListener=null,this.animationListener=null,ie(t))}onDestroy(){this.config&&this.config.ripple()&&this.remove()}static \u0275fac=function(e){return new(e||o)};static \u0275dir=D({type:o,selectors:[["","pRipple",""]],hostAttrs:[1,"p-ripple"],features:[_([Ne]),C]})}return o})();var Fe=`
    .p-button {
        display: inline-flex;
        cursor: pointer;
        user-select: none;
        align-items: center;
        justify-content: center;
        overflow: hidden;
        position: relative;
        color: dt('button.primary.color');
        background: dt('button.primary.background');
        border: 1px solid dt('button.primary.border.color');
        padding: dt('button.padding.y') dt('button.padding.x');
        font-size: 1rem;
        font-family: inherit;
        font-feature-settings: inherit;
        transition:
            background dt('button.transition.duration'),
            color dt('button.transition.duration'),
            border-color dt('button.transition.duration'),
            outline-color dt('button.transition.duration'),
            box-shadow dt('button.transition.duration');
        border-radius: dt('button.border.radius');
        outline-color: transparent;
        gap: dt('button.gap');
    }

    .p-button:disabled {
        cursor: default;
    }

    .p-button-icon-right {
        order: 1;
    }

    .p-button-icon-right:dir(rtl) {
        order: -1;
    }

    .p-button:not(.p-button-vertical) .p-button-icon:not(.p-button-icon-right):dir(rtl) {
        order: 1;
    }

    .p-button-icon-bottom {
        order: 2;
    }

    .p-button-icon-only {
        width: dt('button.icon.only.width');
        padding-inline-start: 0;
        padding-inline-end: 0;
        gap: 0;
    }

    .p-button-icon-only.p-button-rounded {
        border-radius: 50%;
        height: dt('button.icon.only.width');
    }

    .p-button-icon-only .p-button-label {
        visibility: hidden;
        width: 0;
    }

    .p-button-icon-only::after {
        content: "\xA0";
        visibility: hidden;
        width: 0;
    }

    .p-button-sm {
        font-size: dt('button.sm.font.size');
        padding: dt('button.sm.padding.y') dt('button.sm.padding.x');
    }

    .p-button-sm .p-button-icon {
        font-size: dt('button.sm.font.size');
    }

    .p-button-sm.p-button-icon-only {
        width: dt('button.sm.icon.only.width');
    }

    .p-button-sm.p-button-icon-only.p-button-rounded {
        height: dt('button.sm.icon.only.width');
    }

    .p-button-lg {
        font-size: dt('button.lg.font.size');
        padding: dt('button.lg.padding.y') dt('button.lg.padding.x');
    }

    .p-button-lg .p-button-icon {
        font-size: dt('button.lg.font.size');
    }

    .p-button-lg.p-button-icon-only {
        width: dt('button.lg.icon.only.width');
    }

    .p-button-lg.p-button-icon-only.p-button-rounded {
        height: dt('button.lg.icon.only.width');
    }

    .p-button-vertical {
        flex-direction: column;
    }

    .p-button-label {
        font-weight: dt('button.label.font.weight');
    }

    .p-button-fluid {
        width: 100%;
    }

    .p-button-fluid.p-button-icon-only {
        width: dt('button.icon.only.width');
    }

    .p-button:not(:disabled):hover {
        background: dt('button.primary.hover.background');
        border: 1px solid dt('button.primary.hover.border.color');
        color: dt('button.primary.hover.color');
    }

    .p-button:not(:disabled):active {
        background: dt('button.primary.active.background');
        border: 1px solid dt('button.primary.active.border.color');
        color: dt('button.primary.active.color');
    }

    .p-button:focus-visible {
        box-shadow: dt('button.primary.focus.ring.shadow');
        outline: dt('button.focus.ring.width') dt('button.focus.ring.style') dt('button.primary.focus.ring.color');
        outline-offset: dt('button.focus.ring.offset');
    }

    .p-button .p-badge {
        min-width: dt('button.badge.size');
        height: dt('button.badge.size');
        line-height: dt('button.badge.size');
    }

    .p-button-raised {
        box-shadow: dt('button.raised.shadow');
    }

    .p-button-rounded {
        border-radius: dt('button.rounded.border.radius');
    }

    .p-button-secondary {
        background: dt('button.secondary.background');
        border: 1px solid dt('button.secondary.border.color');
        color: dt('button.secondary.color');
    }

    .p-button-secondary:not(:disabled):hover {
        background: dt('button.secondary.hover.background');
        border: 1px solid dt('button.secondary.hover.border.color');
        color: dt('button.secondary.hover.color');
    }

    .p-button-secondary:not(:disabled):active {
        background: dt('button.secondary.active.background');
        border: 1px solid dt('button.secondary.active.border.color');
        color: dt('button.secondary.active.color');
    }

    .p-button-secondary:focus-visible {
        outline-color: dt('button.secondary.focus.ring.color');
        box-shadow: dt('button.secondary.focus.ring.shadow');
    }

    .p-button-success {
        background: dt('button.success.background');
        border: 1px solid dt('button.success.border.color');
        color: dt('button.success.color');
    }

    .p-button-success:not(:disabled):hover {
        background: dt('button.success.hover.background');
        border: 1px solid dt('button.success.hover.border.color');
        color: dt('button.success.hover.color');
    }

    .p-button-success:not(:disabled):active {
        background: dt('button.success.active.background');
        border: 1px solid dt('button.success.active.border.color');
        color: dt('button.success.active.color');
    }

    .p-button-success:focus-visible {
        outline-color: dt('button.success.focus.ring.color');
        box-shadow: dt('button.success.focus.ring.shadow');
    }

    .p-button-info {
        background: dt('button.info.background');
        border: 1px solid dt('button.info.border.color');
        color: dt('button.info.color');
    }

    .p-button-info:not(:disabled):hover {
        background: dt('button.info.hover.background');
        border: 1px solid dt('button.info.hover.border.color');
        color: dt('button.info.hover.color');
    }

    .p-button-info:not(:disabled):active {
        background: dt('button.info.active.background');
        border: 1px solid dt('button.info.active.border.color');
        color: dt('button.info.active.color');
    }

    .p-button-info:focus-visible {
        outline-color: dt('button.info.focus.ring.color');
        box-shadow: dt('button.info.focus.ring.shadow');
    }

    .p-button-warn {
        background: dt('button.warn.background');
        border: 1px solid dt('button.warn.border.color');
        color: dt('button.warn.color');
    }

    .p-button-warn:not(:disabled):hover {
        background: dt('button.warn.hover.background');
        border: 1px solid dt('button.warn.hover.border.color');
        color: dt('button.warn.hover.color');
    }

    .p-button-warn:not(:disabled):active {
        background: dt('button.warn.active.background');
        border: 1px solid dt('button.warn.active.border.color');
        color: dt('button.warn.active.color');
    }

    .p-button-warn:focus-visible {
        outline-color: dt('button.warn.focus.ring.color');
        box-shadow: dt('button.warn.focus.ring.shadow');
    }

    .p-button-help {
        background: dt('button.help.background');
        border: 1px solid dt('button.help.border.color');
        color: dt('button.help.color');
    }

    .p-button-help:not(:disabled):hover {
        background: dt('button.help.hover.background');
        border: 1px solid dt('button.help.hover.border.color');
        color: dt('button.help.hover.color');
    }

    .p-button-help:not(:disabled):active {
        background: dt('button.help.active.background');
        border: 1px solid dt('button.help.active.border.color');
        color: dt('button.help.active.color');
    }

    .p-button-help:focus-visible {
        outline-color: dt('button.help.focus.ring.color');
        box-shadow: dt('button.help.focus.ring.shadow');
    }

    .p-button-danger {
        background: dt('button.danger.background');
        border: 1px solid dt('button.danger.border.color');
        color: dt('button.danger.color');
    }

    .p-button-danger:not(:disabled):hover {
        background: dt('button.danger.hover.background');
        border: 1px solid dt('button.danger.hover.border.color');
        color: dt('button.danger.hover.color');
    }

    .p-button-danger:not(:disabled):active {
        background: dt('button.danger.active.background');
        border: 1px solid dt('button.danger.active.border.color');
        color: dt('button.danger.active.color');
    }

    .p-button-danger:focus-visible {
        outline-color: dt('button.danger.focus.ring.color');
        box-shadow: dt('button.danger.focus.ring.shadow');
    }

    .p-button-contrast {
        background: dt('button.contrast.background');
        border: 1px solid dt('button.contrast.border.color');
        color: dt('button.contrast.color');
    }

    .p-button-contrast:not(:disabled):hover {
        background: dt('button.contrast.hover.background');
        border: 1px solid dt('button.contrast.hover.border.color');
        color: dt('button.contrast.hover.color');
    }

    .p-button-contrast:not(:disabled):active {
        background: dt('button.contrast.active.background');
        border: 1px solid dt('button.contrast.active.border.color');
        color: dt('button.contrast.active.color');
    }

    .p-button-contrast:focus-visible {
        outline-color: dt('button.contrast.focus.ring.color');
        box-shadow: dt('button.contrast.focus.ring.shadow');
    }

    .p-button-outlined {
        background: transparent;
        border-color: dt('button.outlined.primary.border.color');
        color: dt('button.outlined.primary.color');
    }

    .p-button-outlined:not(:disabled):hover {
        background: dt('button.outlined.primary.hover.background');
        border-color: dt('button.outlined.primary.border.color');
        color: dt('button.outlined.primary.color');
    }

    .p-button-outlined:not(:disabled):active {
        background: dt('button.outlined.primary.active.background');
        border-color: dt('button.outlined.primary.border.color');
        color: dt('button.outlined.primary.color');
    }

    .p-button-outlined.p-button-secondary {
        border-color: dt('button.outlined.secondary.border.color');
        color: dt('button.outlined.secondary.color');
    }

    .p-button-outlined.p-button-secondary:not(:disabled):hover {
        background: dt('button.outlined.secondary.hover.background');
        border-color: dt('button.outlined.secondary.border.color');
        color: dt('button.outlined.secondary.color');
    }

    .p-button-outlined.p-button-secondary:not(:disabled):active {
        background: dt('button.outlined.secondary.active.background');
        border-color: dt('button.outlined.secondary.border.color');
        color: dt('button.outlined.secondary.color');
    }

    .p-button-outlined.p-button-success {
        border-color: dt('button.outlined.success.border.color');
        color: dt('button.outlined.success.color');
    }

    .p-button-outlined.p-button-success:not(:disabled):hover {
        background: dt('button.outlined.success.hover.background');
        border-color: dt('button.outlined.success.border.color');
        color: dt('button.outlined.success.color');
    }

    .p-button-outlined.p-button-success:not(:disabled):active {
        background: dt('button.outlined.success.active.background');
        border-color: dt('button.outlined.success.border.color');
        color: dt('button.outlined.success.color');
    }

    .p-button-outlined.p-button-info {
        border-color: dt('button.outlined.info.border.color');
        color: dt('button.outlined.info.color');
    }

    .p-button-outlined.p-button-info:not(:disabled):hover {
        background: dt('button.outlined.info.hover.background');
        border-color: dt('button.outlined.info.border.color');
        color: dt('button.outlined.info.color');
    }

    .p-button-outlined.p-button-info:not(:disabled):active {
        background: dt('button.outlined.info.active.background');
        border-color: dt('button.outlined.info.border.color');
        color: dt('button.outlined.info.color');
    }

    .p-button-outlined.p-button-warn {
        border-color: dt('button.outlined.warn.border.color');
        color: dt('button.outlined.warn.color');
    }

    .p-button-outlined.p-button-warn:not(:disabled):hover {
        background: dt('button.outlined.warn.hover.background');
        border-color: dt('button.outlined.warn.border.color');
        color: dt('button.outlined.warn.color');
    }

    .p-button-outlined.p-button-warn:not(:disabled):active {
        background: dt('button.outlined.warn.active.background');
        border-color: dt('button.outlined.warn.border.color');
        color: dt('button.outlined.warn.color');
    }

    .p-button-outlined.p-button-help {
        border-color: dt('button.outlined.help.border.color');
        color: dt('button.outlined.help.color');
    }

    .p-button-outlined.p-button-help:not(:disabled):hover {
        background: dt('button.outlined.help.hover.background');
        border-color: dt('button.outlined.help.border.color');
        color: dt('button.outlined.help.color');
    }

    .p-button-outlined.p-button-help:not(:disabled):active {
        background: dt('button.outlined.help.active.background');
        border-color: dt('button.outlined.help.border.color');
        color: dt('button.outlined.help.color');
    }

    .p-button-outlined.p-button-danger {
        border-color: dt('button.outlined.danger.border.color');
        color: dt('button.outlined.danger.color');
    }

    .p-button-outlined.p-button-danger:not(:disabled):hover {
        background: dt('button.outlined.danger.hover.background');
        border-color: dt('button.outlined.danger.border.color');
        color: dt('button.outlined.danger.color');
    }

    .p-button-outlined.p-button-danger:not(:disabled):active {
        background: dt('button.outlined.danger.active.background');
        border-color: dt('button.outlined.danger.border.color');
        color: dt('button.outlined.danger.color');
    }

    .p-button-outlined.p-button-contrast {
        border-color: dt('button.outlined.contrast.border.color');
        color: dt('button.outlined.contrast.color');
    }

    .p-button-outlined.p-button-contrast:not(:disabled):hover {
        background: dt('button.outlined.contrast.hover.background');
        border-color: dt('button.outlined.contrast.border.color');
        color: dt('button.outlined.contrast.color');
    }

    .p-button-outlined.p-button-contrast:not(:disabled):active {
        background: dt('button.outlined.contrast.active.background');
        border-color: dt('button.outlined.contrast.border.color');
        color: dt('button.outlined.contrast.color');
    }

    .p-button-outlined.p-button-plain {
        border-color: dt('button.outlined.plain.border.color');
        color: dt('button.outlined.plain.color');
    }

    .p-button-outlined.p-button-plain:not(:disabled):hover {
        background: dt('button.outlined.plain.hover.background');
        border-color: dt('button.outlined.plain.border.color');
        color: dt('button.outlined.plain.color');
    }

    .p-button-outlined.p-button-plain:not(:disabled):active {
        background: dt('button.outlined.plain.active.background');
        border-color: dt('button.outlined.plain.border.color');
        color: dt('button.outlined.plain.color');
    }

    .p-button-text {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.primary.color');
    }

    .p-button-text:not(:disabled):hover {
        background: dt('button.text.primary.hover.background');
        border-color: transparent;
        color: dt('button.text.primary.color');
    }

    .p-button-text:not(:disabled):active {
        background: dt('button.text.primary.active.background');
        border-color: transparent;
        color: dt('button.text.primary.color');
    }

    .p-button-text.p-button-secondary {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.secondary.color');
    }

    .p-button-text.p-button-secondary:not(:disabled):hover {
        background: dt('button.text.secondary.hover.background');
        border-color: transparent;
        color: dt('button.text.secondary.color');
    }

    .p-button-text.p-button-secondary:not(:disabled):active {
        background: dt('button.text.secondary.active.background');
        border-color: transparent;
        color: dt('button.text.secondary.color');
    }

    .p-button-text.p-button-success {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.success.color');
    }

    .p-button-text.p-button-success:not(:disabled):hover {
        background: dt('button.text.success.hover.background');
        border-color: transparent;
        color: dt('button.text.success.color');
    }

    .p-button-text.p-button-success:not(:disabled):active {
        background: dt('button.text.success.active.background');
        border-color: transparent;
        color: dt('button.text.success.color');
    }

    .p-button-text.p-button-info {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.info.color');
    }

    .p-button-text.p-button-info:not(:disabled):hover {
        background: dt('button.text.info.hover.background');
        border-color: transparent;
        color: dt('button.text.info.color');
    }

    .p-button-text.p-button-info:not(:disabled):active {
        background: dt('button.text.info.active.background');
        border-color: transparent;
        color: dt('button.text.info.color');
    }

    .p-button-text.p-button-warn {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.warn.color');
    }

    .p-button-text.p-button-warn:not(:disabled):hover {
        background: dt('button.text.warn.hover.background');
        border-color: transparent;
        color: dt('button.text.warn.color');
    }

    .p-button-text.p-button-warn:not(:disabled):active {
        background: dt('button.text.warn.active.background');
        border-color: transparent;
        color: dt('button.text.warn.color');
    }

    .p-button-text.p-button-help {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.help.color');
    }

    .p-button-text.p-button-help:not(:disabled):hover {
        background: dt('button.text.help.hover.background');
        border-color: transparent;
        color: dt('button.text.help.color');
    }

    .p-button-text.p-button-help:not(:disabled):active {
        background: dt('button.text.help.active.background');
        border-color: transparent;
        color: dt('button.text.help.color');
    }

    .p-button-text.p-button-danger {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.danger.color');
    }

    .p-button-text.p-button-danger:not(:disabled):hover {
        background: dt('button.text.danger.hover.background');
        border-color: transparent;
        color: dt('button.text.danger.color');
    }

    .p-button-text.p-button-danger:not(:disabled):active {
        background: dt('button.text.danger.active.background');
        border-color: transparent;
        color: dt('button.text.danger.color');
    }

    .p-button-text.p-button-contrast {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.contrast.color');
    }

    .p-button-text.p-button-contrast:not(:disabled):hover {
        background: dt('button.text.contrast.hover.background');
        border-color: transparent;
        color: dt('button.text.contrast.color');
    }

    .p-button-text.p-button-contrast:not(:disabled):active {
        background: dt('button.text.contrast.active.background');
        border-color: transparent;
        color: dt('button.text.contrast.color');
    }

    .p-button-text.p-button-plain {
        background: transparent;
        border-color: transparent;
        color: dt('button.text.plain.color');
    }

    .p-button-text.p-button-plain:not(:disabled):hover {
        background: dt('button.text.plain.hover.background');
        border-color: transparent;
        color: dt('button.text.plain.color');
    }

    .p-button-text.p-button-plain:not(:disabled):active {
        background: dt('button.text.plain.active.background');
        border-color: transparent;
        color: dt('button.text.plain.color');
    }

    .p-button-link {
        background: transparent;
        border-color: transparent;
        color: dt('button.link.color');
    }

    .p-button-link:not(:disabled):hover {
        background: transparent;
        border-color: transparent;
        color: dt('button.link.hover.color');
    }

    .p-button-link:not(:disabled):hover .p-button-label {
        text-decoration: underline;
    }

    .p-button-link:not(:disabled):active {
        background: transparent;
        border-color: transparent;
        color: dt('button.link.active.color');
    }
`;var tn=["content"],en=["loadingicon"],nn=["icon"],on=["*"],Ae=(o,l)=>({class:o,pt:l});function rn(o,l){o&1&&jt(0)}function sn(o,l){if(o&1&&et(0,"span",7),o&2){let t=E(3);w(t.cn(t.cx("loadingIcon"),"pi-spin",t.loadingIcon||(t.buttonProps==null?null:t.buttonProps.loadingIcon))),g("pBind",t.ptm("loadingIcon")),N("aria-hidden",!0)}}function ln(o,l){if(o&1&&(dt(),et(0,"svg",8)),o&2){let t=E(3);w(t.cn(t.cx("loadingIcon"),t.cx("spinnerIcon"))),g("pBind",t.ptm("loadingIcon"))("spin",!0),N("aria-hidden",!0)}}function an(o,l){if(o&1&&(ht(0),R(1,sn,1,4,"span",3)(2,ln,1,5,"svg",6),gt()),o&2){let t=E(2);y(),g("ngIf",t.loadingIcon||(t.buttonProps==null?null:t.buttonProps.loadingIcon)),y(),g("ngIf",!(t.loadingIcon||t.buttonProps!=null&&t.buttonProps.loadingIcon))}}function dn(o,l){}function un(o,l){if(o&1&&R(0,dn,0,0,"ng-template",9),o&2){let t=E(2);g("ngIf",t.loadingIconTemplate||t._loadingIconTemplate)}}function cn(o,l){if(o&1&&(ht(0),R(1,an,3,2,"ng-container",2)(2,un,1,1,null,5),gt()),o&2){let t=E();y(),g("ngIf",!t.loadingIconTemplate&&!t._loadingIconTemplate),y(),g("ngTemplateOutlet",t.loadingIconTemplate||t._loadingIconTemplate)("ngTemplateOutletContext",Pt(3,Ae,t.cx("loadingIcon"),t.ptm("loadingIcon")))}}function pn(o,l){if(o&1&&et(0,"span",7),o&2){let t=E(2);w(t.cn(t.cx("icon"),t.icon||(t.buttonProps==null?null:t.buttonProps.icon))),g("pBind",t.ptm("icon")),N("data-p",t.dataIconP)}}function bn(o,l){}function hn(o,l){if(o&1&&R(0,bn,0,0,"ng-template",9),o&2){let t=E(2);g("ngIf",!t.icon&&(t.iconTemplate||t._iconTemplate))}}function gn(o,l){if(o&1&&(ht(0),R(1,pn,1,4,"span",3)(2,hn,1,1,null,5),gt()),o&2){let t=E();y(),g("ngIf",(t.icon||(t.buttonProps==null?null:t.buttonProps.icon))&&!t.iconTemplate&&!t._iconTemplate),y(),g("ngTemplateOutlet",t.iconTemplate||t._iconTemplate)("ngTemplateOutletContext",Pt(3,Ae,t.cx("icon"),t.ptm("icon")))}}function fn(o,l){if(o&1&&(St(0,"span",7),ft(1),xt()),o&2){let t=E();w(t.cx("label")),g("pBind",t.ptm("label")),N("aria-hidden",(t.icon||(t.buttonProps==null?null:t.buttonProps.icon))&&!(t.label||t.buttonProps!=null&&t.buttonProps.label))("data-p",t.dataLabelP),y(),mt(t.label||(t.buttonProps==null?null:t.buttonProps.label))}}function mn(o,l){if(o&1&&et(0,"p-badge",10),o&2){let t=E();g("value",t.badge||(t.buttonProps==null?null:t.buttonProps.badge))("severity",t.badgeSeverity||(t.buttonProps==null?null:t.buttonProps.badgeSeverity))("pt",t.ptm("pcBadge"))("unstyled",t.unstyled())}}var yn={root:({instance:o})=>["p-button p-component",{"p-button-icon-only":o.hasIcon&&!o.label&&!o.buttonProps?.label&&!o.badge,"p-button-vertical":(o.iconPos==="top"||o.iconPos==="bottom")&&o.label,"p-button-loading":o.loading||o.buttonProps?.loading,"p-button-link":o.link||o.buttonProps?.link,[`p-button-${o.severity||o.buttonProps?.severity}`]:o.severity||o.buttonProps?.severity,"p-button-raised":o.raised||o.buttonProps?.raised,"p-button-rounded":o.rounded||o.buttonProps?.rounded,"p-button-text":o.text||o.variant==="text"||o.buttonProps?.text||o.buttonProps?.variant==="text","p-button-outlined":o.outlined||o.variant==="outlined"||o.buttonProps?.outlined||o.buttonProps?.variant==="outlined","p-button-sm":o.size==="small"||o.buttonProps?.size==="small","p-button-lg":o.size==="large"||o.buttonProps?.size==="large","p-button-plain":o.plain||o.buttonProps?.plain,"p-button-fluid":o.hasFluid}],loadingIcon:"p-button-loading-icon",icon:({instance:o})=>["p-button-icon",{[`p-button-icon-${o.iconPos||o.buttonProps?.iconPos}`]:o.label||o.buttonProps?.label,"p-button-icon-left":(o.iconPos==="left"||o.buttonProps?.iconPos==="left")&&o.label||o.buttonProps?.label,"p-button-icon-right":(o.iconPos==="right"||o.buttonProps?.iconPos==="right")&&o.label||o.buttonProps?.label,"p-button-icon-top":(o.iconPos==="top"||o.buttonProps?.iconPos==="top")&&o.label||o.buttonProps?.label,"p-button-icon-bottom":(o.iconPos==="bottom"||o.buttonProps?.iconPos==="bottom")&&o.label||o.buttonProps?.label},o.icon,o.buttonProps?.icon],spinnerIcon:({instance:o})=>Object.entries(o.cx("icon")).filter(([,l])=>!!l).reduce((l,[t])=>l+` ${t}`,"p-button-loading-icon"),label:"p-button-label"},Me=(()=>{class o extends S{name="button";style=Fe;classes=yn;static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275prov=k({token:o,factory:o.\u0275fac})}return o})();var Le=new A("BUTTON_INSTANCE");var vn=(()=>{class o extends I{componentName="Button";hostName="";$pcButton=p(Le,{optional:!0,skipSelf:!0})??void 0;bindDirectiveInstance=p(x,{self:!0});_componentStyle=p(Me);onAfterViewChecked(){this.bindDirectiveInstance.setAttrs(this.ptm("host"))}type="button";badge;disabled;raised=!1;rounded=!1;text=!1;plain=!1;outlined=!1;link=!1;tabindex;size;variant;style;styleClass;badgeClass;badgeSeverity="secondary";ariaLabel;autofocus;iconPos="left";icon;label;loading=!1;loadingIcon;severity;buttonProps;fluid=m(void 0,{transform:v});onClick=new ct;onFocus=new ct;onBlur=new ct;contentTemplate;loadingIconTemplate;iconTemplate;templates;pcFluid=p(Pe,{optional:!0,host:!0,skipSelf:!0});get hasFluid(){return this.fluid()??!!this.pcFluid}get hasIcon(){return this.icon||this.buttonProps?.icon||this.iconTemplate||this._iconTemplate||this.loadingIcon||this.loadingIconTemplate||this._loadingIconTemplate}_contentTemplate;_iconTemplate;_loadingIconTemplate;onAfterContentInit(){this.templates?.forEach(t=>{switch(t.getType()){case"content":this._contentTemplate=t.template;break;case"icon":this._iconTemplate=t.template;break;case"loadingicon":this._loadingIconTemplate=t.template;break;default:this._contentTemplate=t.template;break}})}get dataP(){return this.cn({[this.size]:this.size,"icon-only":this.hasIcon&&!this.label&&!this.badge,loading:this.loading,fluid:this.hasFluid,rounded:this.rounded,raised:this.raised,outlined:this.outlined||this.variant==="outlined",text:this.text||this.variant==="text",link:this.link,vertical:(this.iconPos==="top"||this.iconPos==="bottom")&&this.label})}get dataIconP(){return this.cn({[this.iconPos]:this.iconPos,[this.size]:this.size})}get dataLabelP(){return this.cn({[this.size]:this.size,"icon-only":this.hasIcon&&!this.label&&!this.badge})}static \u0275fac=(()=>{let t;return function(n){return(t||(t=h(o)))(n||o)}})();static \u0275cmp=B({type:o,selectors:[["p-button"]],contentQueries:function(e,n,i){if(e&1&&Ut(i,tn,5)(i,en,5)(i,nn,5)(i,ue,4),e&2){let r;nt(r=ot())&&(n.contentTemplate=r.first),nt(r=ot())&&(n.loadingIconTemplate=r.first),nt(r=ot())&&(n.iconTemplate=r.first),nt(r=ot())&&(n.templates=r)}},inputs:{hostName:"hostName",type:"type",badge:"badge",disabled:[2,"disabled","disabled",v],raised:[2,"raised","raised",v],rounded:[2,"rounded","rounded",v],text:[2,"text","text",v],plain:[2,"plain","plain",v],outlined:[2,"outlined","outlined",v],link:[2,"link","link",v],tabindex:[2,"tabindex","tabindex",Zt],size:"size",variant:"variant",style:"style",styleClass:"styleClass",badgeClass:"badgeClass",badgeSeverity:"badgeSeverity",ariaLabel:"ariaLabel",autofocus:[2,"autofocus","autofocus",v],iconPos:"iconPos",icon:"icon",label:"label",loading:[2,"loading","loading",v],loadingIcon:"loadingIcon",severity:"severity",buttonProps:"buttonProps",fluid:[1,"fluid"]},outputs:{onClick:"onClick",onFocus:"onFocus",onBlur:"onBlur"},features:[_([Me,{provide:Le,useExisting:o},{provide:G,useExisting:o}]),Q([x]),C],ngContentSelectors:on,decls:7,vars:17,consts:[["pRipple","",3,"click","focus","blur","ngStyle","disabled","pAutoFocus","pBind"],[4,"ngTemplateOutlet"],[4,"ngIf"],[3,"class","pBind",4,"ngIf"],[3,"value","severity","pt","unstyled",4,"ngIf"],[4,"ngTemplateOutlet","ngTemplateOutletContext"],["data-p-icon","spinner",3,"class","pBind","spin",4,"ngIf"],[3,"pBind"],["data-p-icon","spinner",3,"pBind","spin"],[3,"ngIf"],[3,"value","severity","pt","unstyled"]],template:function(e,n){e&1&&(Z(),St(0,"button",0),Wt("click",function(r){return n.onClick.emit(r)})("focus",function(r){return n.onFocus.emit(r)})("blur",function(r){return n.onBlur.emit(r)}),X(1),R(2,rn,1,0,"ng-container",1)(3,cn,3,6,"ng-container",2)(4,gn,3,6,"ng-container",2)(5,fn,2,6,"span",3)(6,mn,1,4,"p-badge",4),xt()),e&2&&(w(n.cn(n.cx("root"),n.styleClass,n.buttonProps==null?null:n.buttonProps.styleClass)),g("ngStyle",n.style||(n.buttonProps==null?null:n.buttonProps.style))("disabled",n.disabled||n.loading||(n.buttonProps==null?null:n.buttonProps.disabled))("pAutoFocus",n.autofocus||(n.buttonProps==null?null:n.buttonProps.autofocus))("pBind",n.ptm("root")),N("type",n.type||(n.buttonProps==null?null:n.buttonProps.type))("aria-label",n.ariaLabel||(n.buttonProps==null?null:n.buttonProps.ariaLabel))("tabindex",n.tabindex||(n.buttonProps==null?null:n.buttonProps.tabindex))("data-p",n.dataP)("data-p-disabled",n.disabled||n.loading||(n.buttonProps==null?null:n.buttonProps.disabled))("data-p-severity",n.severity||(n.buttonProps==null?null:n.buttonProps.severity)),y(2),g("ngTemplateOutlet",n.contentTemplate||n._contentTemplate),y(),g("ngIf",n.loading||(n.buttonProps==null?null:n.buttonProps.loading)),y(),g("ngIf",!(n.loading||n.buttonProps!=null&&n.buttonProps.loading)),y(),g("ngIf",!n.contentTemplate&&!n._contentTemplate&&(n.label||(n.buttonProps==null?null:n.buttonProps.label))),y(),g("ngIf",!n.contentTemplate&&!n._contentTemplate&&(n.badge||(n.buttonProps==null?null:n.buttonProps.badge))))},dependencies:[W,Xt,Kt,Yt,$e,ve,Be,ke,Lt,z,x],encapsulation:2,changeDetection:0})}return o})(),Pi=(()=>{class o{static \u0275fac=function(e){return new(e||o)};static \u0275mod=M({type:o});static \u0275inj=$({imports:[W,vn,z,z]})}return o})();export{st as a,G as b,I as c,An as d,ye as e,ve as f,x as g,Ce as h,Pe as i,Ee as j,Be as k,$e as l,vn as m,Pi as n};
