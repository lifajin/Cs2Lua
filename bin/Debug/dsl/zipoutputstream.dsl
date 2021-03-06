require("cs2dsl__lualib");
require("cs2dsl__namespaces");
require("cs2dsl__externenums");
require("cs2dsl__interfaces");

class(ZipOutputStream) {
	static_methods {
		__new_object = deffunc(1)args(...){
			return(newobject(ZipOutputStream, typeargs(), typekinds(), "ctor", null, ...));
		};
		get_Instance = deffunc(1)args(){
			return(getstatic(SymbolKind.Field, ZipOutputStream, "s_Instance"));
		};
		cctor = deffunc(0)args(){
			callstatic(ZipOutputStream, "__cctor");
		};
		__cctor = deffunc(0)args(){
			if(ZipOutputStream.__cctor_called){
				return;
			}else{
				ZipOutputStream.__cctor_called = true;
			};
			ZipOutputStream.s_Instance = ;
		};
	};
	static_fields {
		s_Instance = null;
		__cctor_called = false;
	};
	static_props {
		Instance = {
			get = static_methods.get_Instance;
		};
	};
	static_events {};

	instance_methods {
		ctor = deffunc(0)args(this, ms){
			callinstance(this, "__ctor");
			setinstance(SymbolKind.Field, getstatic(SymbolKind.Property, ZipOutputStream, "Instance"), "V", 1);
			callinstance(getstatic(SymbolKind.Property, ZipOutputStream, "Instance"), "Test");
			return(this);
		},
		Test = deffunc(0)args(this){
		};
		__ctor = deffunc(0)args(this){
			if(getinstance(SymbolKind.Field, this, "__ctor_called")){
				return;
			}else{
				setinstance(SymbolKind.Field, this, "__ctor_called", true);
			};
		};
	};
	instance_fields {
			V = 0;
		__ctor_called = false;
	};
	instance_props {};
	instance_events {};

	interfaces {};

	class_info(TypeKind.Class, Accessibility.Internal) {
	};
	method_info {
		ctor(MethodKind.Constructor, Accessibility.Public){
		};
		get_V(MethodKind.PropertyGet, Accessibility.Public){
		};
		set_V(MethodKind.PropertySet, Accessibility.Public){
		};
		Test(MethodKind.Ordinary, Accessibility.Public){
		};
		get_Instance(MethodKind.PropertyGet, Accessibility.Public){
			static(true);
		};
		cctor(MethodKind.StaticConstructor, Accessibility.Private){
			static(true);
		};
	};
	property_info {
		V(Accessibility.Public){
		};
		Instance(Accessibility.Public){
			static(true);
			readonly(true);
		};
	};
	event_info {};
	field_info {
		s_Instance(Accessibility.Private){
			static(true);
		};
	};
};




class(ZipOutputStream.EmbedClass) {
	static_methods {
		__new_object = deffunc(1)args(...){
			return(newobject(ZipOutputStream.EmbedClass, typeargs(), typekinds(), "ctor", null, ...));
		};
		cctor = deffunc(0)args(){
			callstatic(ZipOutputStream.EmbedClass, "__cctor");
		};
		__cctor = deffunc(0)args(){
			if(ZipOutputStream.EmbedClass.__cctor_called){
				return;
			}else{
				ZipOutputStream.EmbedClass.__cctor_called = true;
			};
		};
	};
	static_fields {
		__cctor_called = false;
	};
	static_props {};
	static_events {};

	instance_methods {
		Test = deffunc(0)args(this){
			callinstance(getstatic(SymbolKind.Property, ZipOutputStream, "Instance"), "Test");
		};
		ctor = deffunc(0)args(this){
			callinstance(this, "__ctor");
		};
		__ctor = deffunc(0)args(this){
			if(getinstance(SymbolKind.Field, this, "__ctor_called")){
				return;
			}else{
				setinstance(SymbolKind.Field, this, "__ctor_called", true);
			};
		};
	};
	instance_fields {
		__ctor_called = false;
	};
	instance_props {};
	instance_events {};

	interfaces {};

	class_info(TypeKind.Class, Accessibility.Private) {
	};
	method_info {
		Test(MethodKind.Ordinary, Accessibility.Public){
		};
		ctor(MethodKind.Constructor, Accessibility.Public){
		};
	};
	property_info {};
	event_info {};
	field_info {};
};



