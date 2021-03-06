﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace RoslynTool.CsToDsl
{
    internal partial class CsDslTranslater
    {
        #region 相对简单的表达式
        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            CodeBuilder.Append(GetIndentString());

            VisitToplevelExpression(node.Expression, ";");
        }
        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            CodeBuilder.Append(node.Token.Text);
        }
        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var ci = m_ClassInfoStack.Peek();
            var oper = m_Model.GetOperationEx(node) as IHasOperatorMethodExpression;
            if (null != oper && oper.UsesOperatorMethod) {
                IMethodSymbol msym = oper.OperatorMethod;
                var castOper = oper as IConversionExpression;
                if (null != castOper) {
                    var opd = castOper.Operand as IConversionExpression;
                    InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                    var arglist = new List<ExpressionSyntax>() { node.Left };
                    ii.Init(msym, arglist, m_Model, opd);
                    OutputOperatorInvoke(ii, node);
                }
                else {
                    var boper = oper as IBinaryOperatorExpression;
                    var lopd = null == boper ? null : boper.LeftOperand as IConversionExpression;
                    var ropd = null == boper ? null : boper.RightOperand as IConversionExpression;

                    InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                    var arglist = new List<ExpressionSyntax>() { node.Left, node.Right };
                    ii.Init(msym, arglist, m_Model, lopd, ropd);
                    OutputOperatorInvoke(ii, node);
                }
            }
            else {
                var boper = oper as IBinaryOperatorExpression;
                var lopd = null == boper ? null : boper.LeftOperand as IConversionExpression;
                var ropd = null == boper ? null : boper.RightOperand as IConversionExpression;

                string op = node.OperatorToken.Text;
                string leftType = "null";
                string rightType = "null";
                string leftTypeKind = "null";
                string rightTypeKind = "null";
                if (null != boper) {
                    if (null != boper.LeftOperand) {
                        var ltype = boper.LeftOperand.Type;
                        if (null != ltype) {
                            leftType = ClassInfo.GetFullName(ltype);
                            leftTypeKind = "TypeKind." + ltype.TypeKind.ToString();
                        }
                    }
                    if (null != boper.RightOperand) {
                        var rtype = boper.RightOperand.Type;
                        if (null != rtype) {
                            rightType = ClassInfo.GetFullName(rtype);
                            rightTypeKind = "TypeKind." + rtype.TypeKind.ToString();
                        }
                    }
                }
                ProcessBinaryOperator(node, ref op);
                string functor = string.Empty;
                if (s_BinaryFunctor.TryGetValue(op, out functor)) {
                    CodeBuilder.AppendFormat("{0}(", functor);
                    bool isConvertDsl = false;
                    var typeInfo = m_Model.GetTypeInfo(node.Right);
                    if (op == "as") {
                        var type = typeInfo.Type;
                        var srcOper = m_Model.GetOperationEx(node.Left);
                        if (null != type && null != srcOper && null != srcOper.Type) {
                            if(InvocationInfo.IsDslToObject(type, srcOper.Type)) {
                                isConvertDsl = true;
                                OutputDslToObjectPrefix(type);
                            }
                            else if(InvocationInfo.IsObjectToDsl(type, srcOper.Type)) {
                                isConvertDsl = true;
                                CodeBuilder.Append("objecttodsl(");
                            }
                        }
                    }
                    OutputExpressionSyntax(node.Left, lopd);
                    if (isConvertDsl) {
                        CodeBuilder.Append(")");
                    }
                    CodeBuilder.Append(", ");
                    if (op == "as" || op == "is") {
                        var type = typeInfo.Type;
                        OutputType(type, node, ci, op);
                        CodeBuilder.AppendFormat(", TypeKind.{0}", type.TypeKind);
                    }
                    else if (op == "??") {
                        var rightOper = m_Model.GetOperationEx(node.Right);
                        bool rightIsConst = null != rightOper && rightOper.ConstantValue.HasValue;
                        if (rightIsConst) {
                            CodeBuilder.Append("true, ");
                            OutputExpressionSyntax(node.Right, ropd);
                        }
                        else {
                            CodeBuilder.Append("false, function(){ funcobjret(");
                            OutputExpressionSyntax(node.Right, ropd);
                            CodeBuilder.Append("); }");
                        }
                    }
                    else {
                        OutputExpressionSyntax(node.Right, ropd);
                    }
                    CodeBuilder.Append(")");
                }
                else {
                    bool handled = false;
                    if (op == "==" || op == "!=") {
                        handled = ProcessEqualOrNotEqual(op, node.Left, node.Right, lopd, ropd);
                    }
                    if (!handled) {
                        CodeBuilder.Append("execbinary(");
                        CodeBuilder.AppendFormat("\"{0}\", ", op);
                        OutputExpressionSyntax(node.Left, lopd);
                        CodeBuilder.Append(", ");
                        OutputExpressionSyntax(node.Right, ropd);
                        CodeBuilder.AppendFormat(", {0}, {1}, {2}, {3}", CsDslTranslater.EscapeType(leftType, leftTypeKind), CsDslTranslater.EscapeType(rightType, rightTypeKind), leftTypeKind, rightTypeKind);
                        CodeBuilder.Append(")");
                    }
                }
            }
        }
        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            IConversionExpression opd = null;
            var oper = m_Model.GetOperationEx(node) as IConditionalChoiceExpression;
            if (null != oper) {
                opd = oper.Condition as IConversionExpression;
            }
            var tOper = m_Model.GetOperationEx(node.WhenTrue);
            var fOper = m_Model.GetOperationEx(node.WhenFalse);
            bool trueIsConst = null != tOper && tOper.ConstantValue.HasValue;
            bool falseIsConst = null != fOper && fOper.ConstantValue.HasValue;

            CodeBuilder.Append("condexp(");
            OutputExpressionSyntax(node.Condition, opd);
            if (trueIsConst) {
                CodeBuilder.Append(", true, ");
            }
            else {
                CodeBuilder.Append(", false, function(){ funcobjret(");
            }
            OutputExpressionSyntax(node.WhenTrue);
            if (trueIsConst) {
                CodeBuilder.Append(", ");
            }
            else {
                CodeBuilder.Append("); }, ");
            }
            if (falseIsConst) {
                CodeBuilder.Append("true, ");
            }
            else {
                CodeBuilder.Append("false, function(){ funcobjret(");
            }
            OutputExpressionSyntax(node.WhenFalse);
            if (falseIsConst) {
                CodeBuilder.Append(")");
            }
            else {
                CodeBuilder.Append("); })");
            }
        }
        public override void VisitThisExpression(ThisExpressionSyntax node)
        {
            CodeBuilder.Append("this");
        }
        public override void VisitBaseExpression(BaseExpressionSyntax node)
        {
            var ci = m_ClassInfoStack.Peek();
            CodeBuilder.AppendFormat("getinstance(SymbolKind.Field, this, {0}, \"base\")", ci.Key);
        }
        public override void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            CodeBuilder.Append("( ");
            OutputExpressionSyntax(node.Expression);
            CodeBuilder.Append(" )");
        }
        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            var oper = m_Model.GetOperationEx(node);
            IConversionExpression opd = null;
            var unaryOper = oper as IUnaryOperatorExpression;
            if (null != unaryOper) {
                opd = unaryOper.Operand as IConversionExpression;
            }
            var assignOper = oper as ICompoundAssignmentExpression;
            if (null != assignOper) {
                opd = assignOper.Value as IConversionExpression;
            }
            ITypeSymbol typeSym = null;
            string type = "null";
            string typeKind = "null";
            if (null != unaryOper && null != unaryOper.Operand) {
                typeSym = unaryOper.Operand.Type;
            }
            else if (null != assignOper && null != assignOper.Target) {
                typeSym = assignOper.Target.Type;
            }
            if (null != typeSym) {
                type = ClassInfo.GetFullName(typeSym);
                typeKind = "TypeKind." + typeSym.TypeKind.ToString();
            }
            if (null != unaryOper && unaryOper.UsesOperatorMethod) {
                IMethodSymbol msym = unaryOper.OperatorMethod;
                InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                var arglist = new List<ExpressionSyntax>() { node.Operand };
                ii.Init(msym, arglist, m_Model, opd);
                OutputOperatorInvoke(ii, node);
            }
            else if (null != assignOper && assignOper.UsesOperatorMethod) {
                //++/--的重载调这里
                CodeBuilder.Append("prefixoperator(true, ");
                OutputExpressionSyntax(node.Operand, opd);
                CodeBuilder.Append(", ");
                IMethodSymbol msym = assignOper.OperatorMethod;
                InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                var arglist = new List<ExpressionSyntax>() { node.Operand };
                ii.Init(msym, arglist, m_Model, opd);
                OutputOperatorInvoke(ii, node);
                CodeBuilder.Append(")");
            }
            else {
                string op = node.OperatorToken.Text;
                if (op == "++" || op == "--") {
                    op = op == "++" ? "+" : "-";
                    CodeBuilder.Append("prefixoperator(true, ");
                    OutputExpressionSyntax(node.Operand, opd);
                    CodeBuilder.Append(", ");
                    CodeBuilder.Append("execbinary(");
                    CodeBuilder.AppendFormat("\"{0}\", ", op);
                    OutputExpressionSyntax(node.Operand, opd);
                    CodeBuilder.AppendFormat(", 1, {0}, {1}, {2}, {3}", CsDslTranslater.EscapeType(type, typeKind), CsDslTranslater.EscapeType(type, typeKind), typeKind, typeKind);
                    CodeBuilder.Append("))");
                }
                else {
                    ProcessUnaryOperator(node, ref op);
                    string functor;
                    if (s_UnaryFunctor.TryGetValue(op, out functor)) {
                        CodeBuilder.AppendFormat("{0}(", functor);
                        OutputExpressionSyntax(node.Operand, opd);
                        CodeBuilder.Append(")");
                    }
                    else {
                        CodeBuilder.Append("execunary(");
                        CodeBuilder.AppendFormat("\"{0}\", ", op);
                        OutputExpressionSyntax(node.Operand, opd);
                        CodeBuilder.AppendFormat(", {0}, {1}", CsDslTranslater.EscapeType(type, typeKind), typeKind);
                        CodeBuilder.Append(")");
                    }
                }
            }
        }
        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var oper = m_Model.GetOperationEx(node);
            IConversionExpression opd = null;
            var unaryOper = oper as IUnaryOperatorExpression;
            if (null != unaryOper) {
                opd = unaryOper.Operand as IConversionExpression;
            }
            var assignOper = oper as ICompoundAssignmentExpression;
            if (null != assignOper) {
                opd = assignOper.Value as IConversionExpression;
            }
            ITypeSymbol typeSym = null;
            string type = "null";
            string typeKind = "null";
            if (null != unaryOper && null != unaryOper.Operand) {
                typeSym = unaryOper.Operand.Type;
            }
            else if (null != assignOper && null != assignOper.Target) {
                typeSym = assignOper.Target.Type;
            }
            if (null != typeSym) {
                type = ClassInfo.GetFullName(typeSym);
                typeKind = "TypeKind." + typeSym.TypeKind.ToString();
            }
            if (null != unaryOper && unaryOper.UsesOperatorMethod) {
                IMethodSymbol msym = unaryOper.OperatorMethod;
                InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                var arglist = new List<ExpressionSyntax>() { node.Operand };
                ii.Init(msym, arglist, m_Model, opd);
                OutputOperatorInvoke(ii, node);
            }
            else if (null != assignOper && assignOper.UsesOperatorMethod) {
                //++/--的重载调这里
                string varName = string.Format("__unary_{0}", GetSourcePosForVar(node));
                CodeBuilder.AppendFormat("postfixoperator(true, {0}, ", varName);
                OutputExpressionSyntax(node.Operand, opd);
                CodeBuilder.Append(", ");
                IMethodSymbol msym = assignOper.OperatorMethod;
                InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                var arglist = new List<ExpressionSyntax>() { node.Operand };
                ii.Init(msym, arglist, m_Model, opd);
                OutputOperatorInvoke(ii, node);
                CodeBuilder.Append(")");
            }
            else {
                string op = node.OperatorToken.Text;
                if (op == "++" || op == "--") {
                    op = op == "++" ? "+" : "-";
                    string varName = string.Format("__unary_{0}", GetSourcePosForVar(node));
                    CodeBuilder.AppendFormat("postfixoperator(true, {0}, ", varName);
                    OutputExpressionSyntax(node.Operand, opd);
                    CodeBuilder.Append(", ");
                    CodeBuilder.Append("execbinary(");
                    CodeBuilder.AppendFormat("\"{0}\", ", op);
                    OutputExpressionSyntax(node.Operand, opd);
                    CodeBuilder.AppendFormat(", 1, {0}, {1}, {2}, {3}", CsDslTranslater.EscapeType(type, typeKind), CsDslTranslater.EscapeType(type, typeKind), typeKind, typeKind);
                    CodeBuilder.Append("))");
                }
                else {
                    ProcessUnaryOperator(node, ref op);
                    string functor;
                    if (s_UnaryFunctor.TryGetValue(op, out functor)) {
                        CodeBuilder.AppendFormat("{0}(", functor);
                        OutputExpressionSyntax(node.Operand, opd);
                        CodeBuilder.Append(")");
                    }
                    else {
                        CodeBuilder.Append("execunary(");
                        CodeBuilder.AppendFormat("\"{0}\", ", op);
                        OutputExpressionSyntax(node.Operand, opd);
                        CodeBuilder.AppendFormat(", {0}, {1}", CsDslTranslater.EscapeType(type, typeKind), typeKind);
                        CodeBuilder.Append(")");
                    }
                }
            }
        }
        public override void VisitSizeOfExpression(SizeOfExpressionSyntax node)
        {
            var oper = m_Model.GetOperationEx(node) as ISizeOfExpression;
            if (oper.ConstantValue.HasValue) {
                OutputConstValue(oper.ConstantValue.Value, oper);
            }
            else {
                ReportIllegalType(oper.Type);
            }
        }
        public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            var ci = m_ClassInfoStack.Peek();

            var oper = m_Model.GetOperationEx(node) as ITypeOfExpression;
            var type = oper.TypeOperand;

            TypeChecker.CheckType(m_Model, type, node, GetCurMethodSemanticInfo());

            CodeBuilder.Append("typeof(");
            OutputType(type, node, ci, "typeof");
            CodeBuilder.Append(")");
        }
        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            var ci = m_ClassInfoStack.Peek();

            var oper = m_Model.GetOperationEx(node) as IConversionExpression;
            var opd = oper.Operand as IConversionExpression;
            if (null != oper && oper.UsesOperatorMethod) {
                IMethodSymbol msym = oper.OperatorMethod;
                InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                var arglist = new List<ExpressionSyntax>() { node.Expression };
                ii.Init(msym, arglist, m_Model, opd);
                AddReferenceAndTryDeriveGenericTypeInstance(ci, oper.Type);

                OutputOperatorInvoke(ii, node);
            }
            else {
                CodeBuilder.Append("typecast(");
                var typeInfo = m_Model.GetTypeInfo(node.Type);
                var type = typeInfo.Type;
                var srcOper = m_Model.GetOperationEx(node.Expression);

                bool isConvertDsl = false;
                if (null != type && null != srcOper && null != srcOper.Type) {
                    if (InvocationInfo.IsDslToObject(type, srcOper.Type)) {
                        isConvertDsl = true;
                        OutputDslToObjectPrefix(type);
                    }
                    else if (InvocationInfo.IsObjectToDsl(type, srcOper.Type)) {
                        isConvertDsl = true;
                        CodeBuilder.Append("objecttodsl(");
                    }
                }
                OutputExpressionSyntax(node.Expression, opd);
                if (isConvertDsl) {
                    CodeBuilder.Append(")");
                }

                if (null != srcOper) {
                    TypeChecker.CheckConvert(m_Model, srcOper.Type, type, node, GetCurMethodSemanticInfo());
                }
                else {
                    Log(node, "cast type checker failed !");
                }

                CodeBuilder.Append(", ");
                OutputType(type, node, ci, "cast");
                CodeBuilder.AppendFormat(", TypeKind.{0}", type.TypeKind);
                CodeBuilder.Append(")");
            }
        }
        public override void VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            var typeInfo = m_Model.GetTypeInfo(node.Type);
            OutputDefaultValue(typeInfo.Type);
        }
        #endregion

        #region 相对复杂的表达式
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var ci = m_ClassInfoStack.Peek();

            string op = node.OperatorToken.Text;
            string baseOp = op.Substring(0, op.Length - 1);

            bool needWrapFunction = true;
            var leftMemberAccess = node.Left as MemberAccessExpressionSyntax;
            var leftElementAccess = node.Left as ElementAccessExpressionSyntax;
            var leftCondAccess = node.Left as ConditionalAccessExpressionSyntax;

            var leftOper = m_Model.GetOperationEx(node.Left);
            var leftSymbolInfo = m_Model.GetSymbolInfoEx(node.Left);
            var leftSym = leftSymbolInfo.Symbol;
            var leftPsym = leftSym as IPropertySymbol;
            var leftEsym = leftSym as IEventSymbol;
            var leftFsym = leftSym as IFieldSymbol;
            
            var rightOper = m_Model.GetOperationEx(node.Right);
            var rightSymbolInfo = m_Model.GetSymbolInfoEx(node.Right);
            var rightSym = rightSymbolInfo.Symbol;

            var curMethod = GetCurMethodSemanticInfo();
            if (null != leftOper && null != rightOper) {
                TypeChecker.CheckConvert(m_Model, rightOper.Type, leftOper.Type, node, curMethod);
            }
            else {
                Log(node, "assignment type checker failed ! left oper:{0} right oper:{1}", leftOper, rightOper);
            }

            SpecialAssignmentType specialType = SpecialAssignmentType.None;
            if (null != leftMemberAccess && null != leftPsym) {
                if (!leftPsym.IsStatic) {
                    bool expIsBasicType = false;
                    var expOper = m_Model.GetOperationEx(leftMemberAccess.Expression);
                    if (null != expOper && SymbolTable.IsBasicType(expOper.Type)) {
                        expIsBasicType = true;
                    }
                    if (CheckExplicitInterfaceAccess(leftPsym)) {
                        specialType = SpecialAssignmentType.PropExplicitImplementInterface;
                    }
                    else if (SymbolTable.IsBasicValueProperty(leftPsym) || expIsBasicType) {
                        specialType = SpecialAssignmentType.PropForBasicValueType;
                    }
                }
            }
            bool dslToObject = false;
            if (null != leftOper && null != leftOper.Type && null != rightOper && null != rightOper.Type) {
                dslToObject = InvocationInfo.IsDslToObject(leftOper.Type, rightOper.Type);
            }
            if (specialType == SpecialAssignmentType.PropExplicitImplementInterface || specialType == SpecialAssignmentType.PropForBasicValueType
                || null != leftElementAccess || null != leftCondAccess
                || leftOper.Type.TypeKind == TypeKind.Delegate && (leftSym.Kind != SymbolKind.Local || op != "=")) {
                needWrapFunction = false;
            }
            string assignVar = null;
            if (needWrapFunction) {
                assignVar = string.Format("__assign_{0}", GetSourcePosForVar(node));
                //顶层的赋值语句已经处理，这里的赋值都需要包装成lambda函数的样式
                CodeBuilder.AppendFormat("execclosure(true, {0}, true){{ ", assignVar);
            }
            bool needWrapStruct = null != rightOper && null != rightOper.Type && rightOper.Type.TypeKind == TypeKind.Struct && !dslToObject && !SymbolTable.IsBasicType(rightOper.Type) && !CsDslTranslater.IsImplementationOfSys(rightOper.Type, "IEnumerator");
            if (needWrapStruct) {
                //dsl脚本里的字段赋值前，先把旧值回收到对象池
                if (null != leftSym && SymbolTable.Instance.IsCs2DslSymbol(leftSym) && SymbolTable.Instance.IsFieldSymbolKind(leftSym)) {
                    string fieldType = string.Empty;
                    if (null != leftPsym)
                        fieldType = ClassInfo.GetFullName(leftPsym.Type);
                    else if (null != leftFsym)
                        fieldType = ClassInfo.GetFullName(leftFsym.Type);
                    string className = ClassInfo.GetFullName(leftSym.ContainingType);
                    string memberName = leftSym.Name;
                    if (leftSym.IsStatic) {
                        CodeBuilder.Append(GetIndentString());
                        CodeBuilder.Append("recyclestaticstructfield(");
                        CodeBuilder.Append(fieldType);
                        CodeBuilder.Append(", ");
                        CodeBuilder.Append(className);
                        CodeBuilder.AppendFormat(", \"{0}\");", memberName);
                        CodeBuilder.AppendLine();
                    }
                    else {
                        CodeBuilder.Append(GetIndentString());
                        CodeBuilder.Append("recycleinstancestructfield(");
                        CodeBuilder.Append(fieldType);
                        CodeBuilder.Append(", ");
                        if (null != leftMemberAccess)
                            OutputExpressionSyntax(leftMemberAccess.Expression);
                        else if (IsNewObjMember(memberName))
                            CodeBuilder.Append("newobj");
                        else
                            CodeBuilder.Append("this");
                        CodeBuilder.AppendFormat(", {0}, \"{1}\");", className, memberName);
                        CodeBuilder.AppendLine();
                    }
                }
            }
            VisitAssignment(ci, op, baseOp, node, string.Empty, false, leftOper, leftSym, leftPsym, leftEsym, leftFsym, leftMemberAccess, leftElementAccess, leftCondAccess, specialType, dslToObject);
            if (needWrapStruct) {
                //只有变量赋值与字段赋值需要处理，其它的都在相应的函数调用里处理了
                if (null != rightSym && (rightSym.Kind == SymbolKind.Method || rightSym.Kind == SymbolKind.Property || rightSym.Kind == SymbolKind.Field || rightSym.Kind == SymbolKind.Local || rightSym.Kind == SymbolKind.Parameter) && SymbolTable.Instance.IsCs2DslSymbol(rightSym)) {
                    if (null != leftSym && (leftSym.Kind == SymbolKind.Local || leftSym.Kind == SymbolKind.Parameter || SymbolTable.Instance.IsFieldSymbolKind(leftSym))) {
                        if (SymbolTable.Instance.IsCs2DslSymbol(rightOper.Type)) {
                            CodeBuilder.Append(GetIndentString());
                            OutputExpressionSyntax(node.Left);
                            CodeBuilder.Append(" = wrapstruct(");
                            OutputExpressionSyntax(node.Left);
                            CodeBuilder.AppendFormat(", {0});", ClassInfo.GetFullName(rightOper.Type));
                            CodeBuilder.AppendLine();
                        }
                        else {
                            string ns = ClassInfo.GetNamespaces(rightOper.Type);
                            if (ns != "System") {
                                CodeBuilder.Append(GetIndentString());
                                OutputExpressionSyntax(node.Left);
                                CodeBuilder.Append(" = wrapexternstruct(");
                                OutputExpressionSyntax(node.Left);
                                CodeBuilder.AppendFormat(", {0});", ClassInfo.GetFullName(rightOper.Type));
                                CodeBuilder.AppendLine();
                            }
                        }
                    }
                }
                //dsl脚本里的字段赋值，需要保证该对象不在函数退出时回收
                if (null != leftSym && SymbolTable.Instance.IsCs2DslSymbol(leftSym) && SymbolTable.Instance.IsFieldSymbolKind(leftSym)) {
                    string fieldType = string.Empty;
                    if (null != leftPsym)
                        fieldType = ClassInfo.GetFullName(leftPsym.Type);
                    else if (null != leftFsym)
                        fieldType = ClassInfo.GetFullName(leftFsym.Type);
                    string className = ClassInfo.GetFullName(leftSym.ContainingType);
                    string memberName = leftSym.Name;
                    if (leftSym.IsStatic) {
                        CodeBuilder.Append(GetIndentString());
                        CodeBuilder.Append("keepstaticstructfield(");
                        CodeBuilder.Append(fieldType);
                        CodeBuilder.Append(", ");
                        CodeBuilder.Append(className);
                        CodeBuilder.AppendFormat(", \"{0}\");", memberName);
                        CodeBuilder.AppendLine();
                    }
                    else {
                        CodeBuilder.Append(GetIndentString());
                        CodeBuilder.Append("keepinstancestructfield(");
                        CodeBuilder.Append(fieldType);
                        CodeBuilder.Append(", ");
                        if (null != leftMemberAccess)
                            OutputExpressionSyntax(leftMemberAccess.Expression);
                        else if (IsNewObjMember(memberName))
                            CodeBuilder.Append("newobj");
                        else
                            CodeBuilder.Append("this");
                        CodeBuilder.AppendFormat(", {0}, \"{1}\");", className, memberName);
                        CodeBuilder.AppendLine();
                    }
                }
            }
            if (needWrapFunction) {
                //这里假定赋值语句左边是多次访问不变的左值（暂未想到满足所有情形的解决方案）
                CodeBuilder.AppendFormat("; {0} = ", assignVar);
                OutputExpressionSyntax(node.Left);
                CodeBuilder.Append("; }");
            }
        }
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var ci = m_ClassInfoStack.Peek();
            VisitInvocation(ci, node, string.Empty, string.Empty, false);
        }
        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            TypeChecker.CheckMemberAccess(m_Model, node, GetCurMethodSemanticInfo());

            SymbolInfo symInfo = m_Model.GetSymbolInfoEx(node);
            var sym = symInfo.Symbol;

            string className = string.Empty;
            if (null != sym && null != sym.ContainingType) {
                className = ClassInfo.GetFullName(sym.ContainingType);
            }

            bool isExtern = false;
            if (null != sym) {
                isExtern = !SymbolTable.Instance.IsCs2DslSymbol(sym);
                if (sym.IsStatic) {
                    var ci = m_ClassInfoStack.Peek();
                    AddReferenceAndTryDeriveGenericTypeInstance(ci, sym);
                }
                else {
                    SymbolTable.Instance.TryAddExternReference(sym);
                }
            }
            else {
                ReportIllegalSymbol(node, symInfo);
            }
            if (null != sym) {
                if (node.Parent is InvocationExpressionSyntax) {
                    var msym = sym as IMethodSymbol;
                    string manglingName = NameMangling(msym);
                    if (!sym.IsStatic) {
                        if (isExtern)
                            CodeBuilder.Append("getexterninstance(SymbolKind.");
                        else
                            CodeBuilder.Append("getinstance(SymbolKind.");
                        CodeBuilder.Append(SymbolTable.Instance.GetSymbolKind(sym));
                        CodeBuilder.Append(", ");
                        OutputExpressionSyntax(node.Expression);
                        CodeBuilder.Append(", ");
                        CodeBuilder.Append(className);
                        CodeBuilder.Append(", \"");
                    }
                    else {
                        if (isExtern)
                            CodeBuilder.Append("getexternstatic(SymbolKind.");
                        else
                            CodeBuilder.Append("getstatic(SymbolKind.");
                        CodeBuilder.Append(SymbolTable.Instance.GetSymbolKind(sym));
                        CodeBuilder.Append(", ");
                        CodeBuilder.Append(className);
                        CodeBuilder.Append(", \"");
                    }
                    CodeBuilder.Append(manglingName);
                    CodeBuilder.Append("\")");
                }
                else {
                    var msym = sym as IMethodSymbol;
                    if (null != msym) {
                        string manglingName = NameMangling(msym);
                        string srcPos = GetSourcePosForVar(node);
                        string delegationKey = string.Format("{0}:{1}", ClassInfo.GetFullName(msym.ContainingType), manglingName);
                        if (!sym.IsStatic) {
                            CodeBuilder.AppendFormat("builddelegation(\"{0}\", \"{1}\", ", srcPos, delegationKey);
                            OutputExpressionSyntax(node.Expression);
                            CodeBuilder.AppendFormat(", {0}, {1})", manglingName, "false");
                        }
                        else {
                            CodeBuilder.AppendFormat("builddelegation(\"{0}\", \"{1}\", {2}, {3}, {4})", srcPos, delegationKey, className, manglingName, "true");
                        }
                    }
                    else {
                        var psym = sym as IPropertySymbol;
                        var fsym = sym as IFieldSymbol;
                        string mname = string.Empty;
                        bool propExplicitImplementInterface = false;
                        bool propForBasicValueType = false;
                        if (null != psym) {
                            if (!psym.IsStatic) {
                                propExplicitImplementInterface = CheckExplicitInterfaceAccess(psym, ref mname);
                                var expOper = m_Model.GetOperationEx(node.Expression);
                                bool expIsBasicType = false;
                                if (null != expOper && SymbolTable.IsBasicType(expOper.Type)) {
                                    expIsBasicType = true;
                                }
                                propForBasicValueType = SymbolTable.IsBasicValueProperty(psym) || expIsBasicType;
                            }
                        }
                        if (propExplicitImplementInterface) {
                            //这里不区分是否外部符号了，委托到动态语言的脚本库实现，可根据对象运行时信息判断
                            CodeBuilder.Append("getinstance(SymbolKind.Property, ");
                            OutputExpressionSyntax(node.Expression);
                            CodeBuilder.AppendFormat(", {0}, \"{1}\")", className, mname);
                        }
                        else if (propForBasicValueType) {
                            //这里不区分是否外部符号了，委托到动态语言的脚本库实现，可根据对象运行时信息判断
                            string pname = psym.Name;
                            string cname = ClassInfo.GetFullName(psym.ContainingType);
                            bool isEnumClass = psym.ContainingType.TypeKind == TypeKind.Enum || cname == "System.Enum";
                            var oper = m_Model.GetOperationEx(node.Expression);
                            string ckey = InvocationInfo.CalcInvokeTarget(isEnumClass, cname, this, oper);
                            CodeBuilder.AppendFormat("getforbasicvalue(");
                            OutputExpressionSyntax(node.Expression);
                            CodeBuilder.AppendFormat(", {0}, {1}, \"{2}\")", isEnumClass ? "true" : "false", ckey, pname);
                        }
                        else {
                            bool isExternStructMember = isExtern && (null != psym && psym.Type.TypeKind == TypeKind.Struct && !SymbolTable.IsBasicType(psym.Type) ||
                                    null != fsym && fsym.Type.TypeKind == TypeKind.Struct && !SymbolTable.IsBasicType(fsym.Type));
                            if (!sym.IsStatic) {
                                if (isExternStructMember) {
                                    CodeBuilder.Append("getexterninstancestructmember(SymbolKind.");
                                    CodeBuilder.Append(SymbolTable.Instance.GetSymbolKind(sym));
                                    CodeBuilder.Append(", ");
                                    OutputExpressionSyntax(node.Expression);
                                    CodeBuilder.Append(", ");
                                    CodeBuilder.Append(className);
                                }
                                else {
                                    if (isExtern)
                                        CodeBuilder.Append("getexterninstance(SymbolKind.");
                                    else
                                        CodeBuilder.Append("getinstance(SymbolKind.");
                                    CodeBuilder.Append(SymbolTable.Instance.GetSymbolKind(sym));
                                    CodeBuilder.Append(", ");
                                    OutputExpressionSyntax(node.Expression);
                                    CodeBuilder.Append(", ");
                                    CodeBuilder.Append(className);
                                }
                            }
                            else {
                                if (isExternStructMember) {
                                    CodeBuilder.Append("getexternstaticstructmember(SymbolKind.");
                                    CodeBuilder.Append(SymbolTable.Instance.GetSymbolKind(sym));
                                    CodeBuilder.Append(", ");
                                    CodeBuilder.Append(className);
                                }
                                else {
                                    if (isExtern)
                                        CodeBuilder.Append("getexternstatic(SymbolKind.");
                                    else
                                        CodeBuilder.Append("getstatic(SymbolKind.");
                                    CodeBuilder.Append(SymbolTable.Instance.GetSymbolKind(sym));
                                    CodeBuilder.Append(", ");
                                    CodeBuilder.Append(className);
                                }
                            }
                            CodeBuilder.Append(", ");
                            CodeBuilder.AppendFormat("\"{0}\"", node.Name.Identifier.Text);
                            CodeBuilder.Append(")");
                        }
                    }
                }
            }
            else {
                OutputExpressionSyntax(node.Expression);
                CodeBuilder.Append(node.OperatorToken.Text);
                CodeBuilder.Append(node.Name.Identifier.Text);
            }
        }
        public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            var oper = m_Model.GetOperationEx(node);
            var symInfo = m_Model.GetSymbolInfoEx(node);
            var sym = symInfo.Symbol;
            var psym = sym as IPropertySymbol;
            if (null != sym){
                if (sym.IsStatic) {
                    var ci = m_ClassInfoStack.Peek();
                    AddReferenceAndTryDeriveGenericTypeInstance(ci, sym);
                }
                else {
                    SymbolTable.Instance.TryAddExternReference(sym);
                }
            }
            if (null != psym && psym.IsIndexer) {
                bool isCs2Lua = SymbolTable.Instance.IsCs2DslSymbol(psym);
                CodeBuilder.AppendFormat("get{0}{1}indexer(", isCs2Lua ? string.Empty : "extern", psym.IsStatic ? "static" : "instance");
                if (!isCs2Lua) {
                    INamedTypeSymbol namedTypeSym = null;
                    var expOper = m_Model.GetOperationEx(node.Expression);
                    if (null != expOper) {
                        string fullName = ClassInfo.GetFullName(expOper.Type);
                        CodeBuilder.Append(fullName);
                        namedTypeSym = expOper.Type as INamedTypeSymbol;
                    }
                    else {
                        CodeBuilder.Append("null");
                    }
                    CodeBuilder.Append(", ");
                    OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                    CodeBuilder.Append(", ");
                }
                if (psym.IsStatic) {
                    string fullName = ClassInfo.GetFullName(psym.ContainingType);
                    CodeBuilder.Append(fullName);
                }
                else {
                    OutputExpressionSyntax(node.Expression);
                }
                CodeBuilder.Append(", ");
                string manglingName = NameMangling(psym.GetMethod);
                if (!psym.IsStatic) {
                    CheckExplicitInterfaceAccess(psym.GetMethod, ref manglingName);
                    string fullName = ClassInfo.GetFullName(psym.ContainingType);
                    CodeBuilder.Append(fullName);
                    CodeBuilder.Append(", ");
                }
                CodeBuilder.AppendFormat("\"{0}\", {1}, ", manglingName, psym.GetMethod.Parameters.Length);
                InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                ii.Init(psym.GetMethod, node.ArgumentList, m_Model);
                OutputArgumentList(ii, false, node);
                CodeBuilder.Append(")");
            }
            else if (oper.Kind == OperationKind.ArrayElementReferenceExpression) {
                if (SymbolTable.UseArrayGetSet) {
                    CodeBuilder.Append("arrayget(");
                    OutputExpressionSyntax(node.Expression);
                    CodeBuilder.Append(", ");
                    VisitBracketedArgumentList(node.ArgumentList);
                    CodeBuilder.Append(")");
                }
                else {
                    OutputExpressionSyntax(node.Expression);
                    CodeBuilder.Append("[");
                    VisitBracketedArgumentList(node.ArgumentList);
                    CodeBuilder.Append("]");
                }
            }
            else {
                ReportIllegalSymbol(node, symInfo);
            }
        }
        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            CodeBuilder.Append("condaccess(");
            OutputExpressionSyntax(node.Expression);
            CodeBuilder.Append(", ");
            var elementBinding = node.WhenNotNull as ElementBindingExpressionSyntax;
            if (null != elementBinding) {
                var oper = m_Model.GetOperationEx(node.WhenNotNull);
                var symInfo = m_Model.GetSymbolInfoEx(node.WhenNotNull);
                var sym = symInfo.Symbol;
                var psym = sym as IPropertySymbol;
                if (null != sym){
                    if (sym.IsStatic) {
                        var ci = m_ClassInfoStack.Peek();
                        AddReferenceAndTryDeriveGenericTypeInstance(ci, sym);
                    }
                    else {
                        SymbolTable.Instance.TryAddExternReference(sym);
                    }
                }
                if (null != psym && psym.IsIndexer) {
                    CodeBuilder.Append("function(){ funcobjret(");
                    bool isCs2Lua = SymbolTable.Instance.IsCs2DslSymbol(psym);
                    CodeBuilder.AppendFormat("get{0}{1}indexer(", isCs2Lua ? string.Empty : "extern", psym.IsStatic ? "static" : "instance");
                    if (!isCs2Lua) {
                        INamedTypeSymbol namedTypeSym = null;
                        var expOper = m_Model.GetOperationEx(node.Expression);
                        if (null != expOper) {
                            string fullName = ClassInfo.GetFullName(expOper.Type);
                            CodeBuilder.Append(fullName);
                            namedTypeSym = expOper.Type as INamedTypeSymbol;
                        }
                        else {
                            CodeBuilder.Append("null");
                        }
                        CodeBuilder.Append(", ");
                        OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                        CodeBuilder.Append(", ");
                    }
                    if (psym.IsStatic) {
                        string fullName = ClassInfo.GetFullName(psym.ContainingType);
                        CodeBuilder.Append(fullName);
                    }
                    else {
                        OutputExpressionSyntax(node.Expression);
                    }
                    CodeBuilder.Append(", ");
                    string manglingName = NameMangling(psym.GetMethod);
                    if (!psym.IsStatic) {
                        CheckExplicitInterfaceAccess(psym.GetMethod, ref manglingName);
                        string fullName = ClassInfo.GetFullName(psym.ContainingType);
                        CodeBuilder.Append(fullName);
                        CodeBuilder.Append(", ");
                    }
                    CodeBuilder.AppendFormat("\"{0}\", {1}, ", manglingName, psym.GetMethod.Parameters.Length);
                    InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                    List<ExpressionSyntax> args = new List<ExpressionSyntax> { node.WhenNotNull };
                    ii.Init(psym.GetMethod, args, m_Model);
                    OutputArgumentList(ii, false, elementBinding);
                    CodeBuilder.Append(")");
                    CodeBuilder.Append("); }");
                }
                else if (oper.Kind == OperationKind.ArrayElementReferenceExpression) {
                    if (SymbolTable.UseArrayGetSet) {
                        CodeBuilder.Append("arrayget(");
                        OutputExpressionSyntax(node.Expression);
                        CodeBuilder.Append(", ");
                        OutputExpressionSyntax(node.WhenNotNull);
                        CodeBuilder.Append(")");
                    }
                    else {
                        CodeBuilder.Append("function(){ funcobjret(");
                        OutputExpressionSyntax(node.Expression);
                        CodeBuilder.Append("[");
                        OutputExpressionSyntax(node.WhenNotNull);
                        CodeBuilder.Append("]");
                        CodeBuilder.Append("); }");
                    }
                }
                else {
                    ReportIllegalSymbol(node, symInfo);
                }
            }
            else {
                CodeBuilder.Append("function(){ funcobjret(");
                OutputExpressionSyntax(node.Expression);
                OutputExpressionSyntax(node.WhenNotNull);
                CodeBuilder.Append("); }");
            }
            CodeBuilder.Append(")");
        }
        public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
        {
            var symInfo = m_Model.GetSymbolInfoEx(node.Name);
            var sym = symInfo.Symbol;
            var msym = sym as IMethodSymbol;
            if (null != msym) {
                string manglingName = NameMangling(msym);
                CodeBuilder.AppendFormat(".{0}", manglingName);
            }
            else {
                CodeBuilder.AppendFormat("{0}{1}", node.OperatorToken.Text, node.Name.Identifier.Text);
            }
        }
        public override void VisitElementBindingExpression(ElementBindingExpressionSyntax node)
        {
            VisitBracketedArgumentList(node.ArgumentList);
        }
        public override void VisitImplicitElementAccess(ImplicitElementAccessSyntax node)
        {
            CodeBuilder.Append("[");
            VisitBracketedArgumentList(node.ArgumentList);
            CodeBuilder.Append("]");
        }
        /// <summary>
        /// 对象创建与初始化是除赋值与函数调外另一个比较复杂的地方，这个函数和后面几个函数完成相关工作。
        /// </summary>
        /// <param name="node"></param>
        public override void VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            var oper = m_Model.GetOperationEx(node);
            if (null != oper) {
                var namedTypeSym = oper.Type as INamedTypeSymbol;
                bool isCollection = node.IsKind(SyntaxKind.CollectionInitializerExpression);
                bool isObjectInitializer = node.IsKind(SyntaxKind.ObjectInitializerExpression);
                bool isArray = node.IsKind(SyntaxKind.ArrayInitializerExpression);
                bool isComplex = node.IsKind(SyntaxKind.ComplexElementInitializerExpression);
                if (isCollection) {
                    //调用BoundCollectionInitializerExpression.Initializers
                    var inits = oper.GetType().InvokeMember("Initializers", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, oper, null) as IList;
                    if (null != oper.Type) {
                        bool isDictionary = IsImplementationOfSys(oper.Type, "IDictionary");
                        bool isList = IsImplementationOfSys(oper.Type, "IList");
                        if (isDictionary) {
                            var args = node.Expressions;
                            int ct = args.Count;
                            CodeBuilder.Append("literaldictionary(");
                            CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                            if (ct > 0)
                                CodeBuilder.Append(", ");
                            for (int i = 0; i < ct; ++i) {
                                var exp = args[i] as InitializerExpressionSyntax;
                                if (null != exp) {
                                    IConversionExpression opd1 = null, opd2 = null;
                                    if (null != inits && i < inits.Count) {
                                        var init = inits[i];
                                        //调用BoundCollectionElementInitializer.Arguments
                                        var initOpers = init.GetType().InvokeMember("Arguments", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, init, null) as IList;
                                        if (null != initOpers) {
                                            if (0 < initOpers.Count) {
                                                opd1 = initOpers[0] as IConversionExpression;
                                            }
                                            if (1 < initOpers.Count) {
                                                opd2 = initOpers[1] as IConversionExpression;
                                            }
                                        }
                                    }
                                    OutputExpressionSyntax(exp.Expressions[0], opd1);
                                    CodeBuilder.Append(" => ");
                                    OutputExpressionSyntax(exp.Expressions[1], opd2);
                                }
                                else {
                                    Log(args[i], "Dictionary init error !");
                                }
                                if (i < ct - 1) {
                                    CodeBuilder.Append(", ");
                                }
                            }
                            CodeBuilder.Append(")");
                        }
                        else if (isList) {
                            var args = node.Expressions;
                            int ct = args.Count;
                            CodeBuilder.Append("literallist(");
                            CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                            if (ct > 0)
                                CodeBuilder.Append(", ");
                            for (int i = 0; i < ct; ++i) {
                                IConversionExpression opd = null;
                                if (null != inits && i < inits.Count) {
                                    var init = inits[i];
                                    //调用BoundCollectionElementInitializer.Arguments
                                    var initOpers = init.GetType().InvokeMember("Arguments", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, init, null) as IList;
                                    if (null != initOpers) {
                                        if (0 < initOpers.Count) {
                                            opd = initOpers[0] as IConversionExpression;
                                        }
                                    }
                                }
                                OutputExpressionSyntax(args[i], opd);
                                if (i < ct - 1) {
                                    CodeBuilder.Append(", ");
                                }
                            }
                            CodeBuilder.Append(")");
                        }
                        else {
                            var args = node.Expressions;
                            int ct = args.Count;
                            CodeBuilder.Append("literalcollection(");
                            CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                            if (ct > 0)
                                CodeBuilder.Append(", ");
                            for (int i = 0; i < ct; ++i) {
                                IConversionExpression opd = null;
                                if (null != inits && i < inits.Count) {
                                    var init = inits[i];
                                    //调用BoundCollectionElementInitializer.Arguments
                                    var initOpers = init.GetType().InvokeMember("Arguments", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, init, null) as IList;
                                    if (null != initOpers) {
                                        if (0 < initOpers.Count) {
                                            opd = initOpers[0] as IConversionExpression;
                                        }
                                    }
                                }
                                OutputExpressionSyntax(args[i], opd);
                                if (i < ct - 1) {
                                    CodeBuilder.Append(", ");
                                }
                            }
                            CodeBuilder.Append(")");
                        }
                    }
                    else {
                        Log(node, "Can't find operation type ! operation info: {0}", oper.ToString());
                    }
                }
                else if (isComplex) {
                    //ComplexElementInitializer，目前未发现roslyn有实际合法的语法实例，执行到这的一般是存在语义错误的语法
                    var args = node.Expressions;
                    int ct = args.Count;
                    CodeBuilder.Append("literalcomplex(");
                    CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                    if (ct > 0)
                        CodeBuilder.Append(", ");
                    for (int i = 0; i < ct; ++i) {
                        OutputExpressionSyntax(args[i], null);
                        if (i < ct - 1) {
                            CodeBuilder.Append(", ");
                        }
                    }
                    CodeBuilder.Append(")");
                }
                else if (isArray) {
                    var args = node.Expressions;
                    var arrInitOper = oper as IArrayInitializer;
                    int ct = args.Count;
                    string elementType = "null";
                    string typeKind = "ErrorType";
                    var arrCreateExp = node.Parent as ArrayCreationExpressionSyntax;
                    if (null != arrCreateExp) {
                        var arrCreate = m_Model.GetOperationEx(arrCreateExp) as IArrayCreationExpression;
                        ITypeSymbol etype = SymbolTable.GetElementType(arrCreate.ElementType);
                        elementType = ClassInfo.GetFullName(etype);
                        typeKind = etype.TypeKind.ToString();
                    }
                    else if (ct > 0) {
                        ITypeSymbol etype = SymbolTable.GetElementType(arrInitOper.ElementValues[0].Type);
                        elementType = ClassInfo.GetFullName(etype);
                        typeKind = etype.TypeKind.ToString();
                    }
                    if (ct > 0) {
                        CodeBuilder.AppendFormat("literalarray({0}, TypeKind.{1}, ", elementType, typeKind);
                        for (int i = 0; i < ct; ++i) {
                            var exp = args[i];
                            IConversionExpression opd = arrInitOper.ElementValues[i] as IConversionExpression;
                            OutputExpressionSyntax(exp, opd);
                            if (i < ct - 1) {
                                CodeBuilder.Append(", ");
                            }
                        }
                        CodeBuilder.Append(")");
                    }
                    else {
                        CodeBuilder.AppendFormat("newarray({0}, TypeKind.{1})", elementType, typeKind);
                    }
                }
                else {
                    //isObjectInitializer
                    bool isCollectionObj = false;
                    var typeSymInfo = oper.Type;
                    if (null != typeSymInfo) {
                        isCollectionObj = IsImplementationOfSys(typeSymInfo, "ICollection");
                    }
                    if (isCollectionObj) {
                        var args = node.Expressions;
                        int ct = args.Count;
                        CodeBuilder.Append("literalcollection(");
                        CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                        if (ct > 0)
                            CodeBuilder.Append(", ");
                        for (int i = 0; i < ct; ++i) {
                            var exp = args[i];
                            IConversionExpression opd = null;
                            //调用BoundObjectInitializerExpression.Initializers
                            var inits = oper.GetType().InvokeMember("Initializers", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, oper, null) as IList;
                            if (null != inits && i < inits.Count) {
                                opd = inits[i] as IConversionExpression;
                            }
                            if (args[i] is AssignmentExpressionSyntax) {
                                VisitToplevelExpression(args[i], string.Empty);
                            }
                            else {
                                OutputExpressionSyntax(exp, opd);
                            }
                            if (i < ct - 1)
                                CodeBuilder.Append(",");
                        }
                        CodeBuilder.Append(")");
                    }
                    else {
                        m_ObjectInitializerStack.Push(typeSymInfo);
                        CodeBuilder.Append("function(newobj){ ");
                        var args = node.Expressions;
                        int ct = args.Count;
                        for (int i = 0; i < ct; ++i) {
                            var exp = args[i];
                            IConversionExpression opd = null;
                            //调用BoundObjectInitializerExpression.Initializers
                            var inits = oper.GetType().InvokeMember("Initializers", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, oper, null) as IList;
                            if (null != inits && i < inits.Count) {
                                opd = inits[i] as IConversionExpression;
                            }
                            if (exp is AssignmentExpressionSyntax) {
                                VisitToplevelExpression(exp, string.Empty);
                            }
                            else {
                                OutputExpressionSyntax(exp, opd);
                            }
                            CodeBuilder.Append(";");
                        }
                        CodeBuilder.Append(" }");
                        m_ObjectInitializerStack.Pop();
                    }
                }
            }
            else {
                Log(node, "Can't find operation info !");
            }
        }
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var ci = m_ClassInfoStack.Peek();
            var oper = m_Model.GetOperationEx(node);
            var objectCreate = oper as IObjectCreationExpression;
            if (null != objectCreate) {
                var typeSymInfo = objectCreate.Type;
                var sym = objectCreate.Constructor;

                TypeChecker.CheckType(m_Model, typeSymInfo, node, GetCurMethodSemanticInfo());

                string fullTypeName = ClassInfo.GetFullName(typeSymInfo);
                var namedTypeSym = typeSymInfo as INamedTypeSymbol;

                //处理ref/out参数
                InvocationInfo ii = new InvocationInfo(GetCurMethodSemanticInfo(), node);
                ii.Init(sym, node.ArgumentList, m_Model);
                AddReferenceAndTryDeriveGenericTypeInstance(ci, sym);

                bool isValueType = typeSymInfo.TypeKind == TypeKind.Struct;
                bool isCollection = IsImplementationOfSys(typeSymInfo, "ICollection");
                bool isExternal = !SymbolTable.Instance.IsCs2DslSymbol(typeSymInfo);

                string ctor = NameMangling(sym);
                string localName = string.Format("__newobject_{0}", GetSourcePosForVar(node));
                if (ii.ReturnArgs.Count > 0) {
                    CodeBuilder.AppendFormat("execclosure(true, {0}, true){{ ", localName);
                    CodeBuilder.AppendFormat("multiassign({0}", localName);
                    CodeBuilder.Append(", ");
                    OutputExpressionList(ii.ReturnArgs, node);
                    CodeBuilder.Append(") = ");
                }
                if (isCollection) {
                    bool isDictionary = IsImplementationOfSys(typeSymInfo, "IDictionary");
                    bool isList = IsImplementationOfSys(typeSymInfo, "IList");
                    if (isDictionary) {
                        //字典对象的处理
                        CodeBuilder.AppendFormat("new{0}dictionary({1}, ", isExternal ? "extern" : string.Empty, fullTypeName);
                        CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                    }
                    else if (isList) {
                        //列表对象的处理
                        CodeBuilder.AppendFormat("new{0}list({1}, ", isExternal ? "extern" : string.Empty, fullTypeName);
                        CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                    }
                    else {
                        //集合对象的处理
                        CodeBuilder.AppendFormat("new{0}collection({1}, ", isExternal ? "extern" : string.Empty, fullTypeName);
                        CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                    }
                }
                else if (isValueType) {
                    CodeBuilder.AppendFormat("new{0}struct({1}, ", isExternal ? "extern" : string.Empty, fullTypeName);
                    CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                }
                else {
                    CodeBuilder.AppendFormat("new{0}object({1}, ", isExternal ? "extern" : string.Empty, fullTypeName);
                    CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                }
                if (!isExternal) {
                    //外部对象函数名不会换名，所以没必要提供名字，总是ctor
                    if (string.IsNullOrEmpty(ctor)) {
                        CodeBuilder.Append(", null");
                    }
                    else {
                        CodeBuilder.AppendFormat(", \"{0}\"", ctor);
                    }
                }
                if (null != node.Initializer) {
                    CodeBuilder.Append(", ");
                    VisitInitializerExpression(node.Initializer);
                }
                else {
                    if (isCollection) {
                        bool isDictionary = IsImplementationOfSys(typeSymInfo, "IDictionary");
                        bool isList = IsImplementationOfSys(typeSymInfo, "IList");
                        if (isDictionary) {
                            CodeBuilder.Append(", literaldictionary(");
                            CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                            CodeBuilder.Append(")");
                        }
                        else if (isList) {
                            CodeBuilder.Append(", literallist(");
                            CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                            CodeBuilder.Append(")");
                        }
                        else {
                            CodeBuilder.Append(", literalcollection(");
                            CsDslTranslater.OutputTypeArgsInfo(CodeBuilder, namedTypeSym);
                            CodeBuilder.Append(")");
                        }
                    }
                    else {
                        CodeBuilder.Append(", null");
                    }
                }
                if (!string.IsNullOrEmpty(ii.ExternOverloadedMethodSignature) || ii.Args.Count + ii.DefaultValueArgs.Count + ii.GenericTypeArgs.Count > 0) {
                    CodeBuilder.Append(", ");
                }
                OutputArgumentList(ii, false, node);
                CodeBuilder.Append(")");
                if (ii.ReturnArgs.Count > 0) {
                    CodeBuilder.Append("; }");
                }
            }
            else {
                var methodBinding = oper as IMethodBindingExpression;
                if (null != methodBinding) {
                    var typeSymInfo = methodBinding.Type;
                    var msym = methodBinding.Method;
                    if (null != msym) {
                        string manglingName = NameMangling(msym);

                        AddReferenceAndTryDeriveGenericTypeInstance(ci, msym);
                        string className = ClassInfo.GetFullName(msym.ContainingType);
                        string srcPos = GetSourcePosForVar(node);
                        string delegationKey = string.Format("{0}:{1}", className, manglingName);
                        if (msym.IsStatic) {
                            CodeBuilder.AppendFormat("builddelegation(\"{0}\", \"{1}\", {2}, {3}, {4})", srcPos, delegationKey, className, manglingName, "true");
                        }
                        else {
                            CodeBuilder.AppendFormat("builddelegation(\"{0}\", \"{1}\", this, {2}, {3})", srcPos, delegationKey, manglingName, "false");
                        }
                    }
                    else {
                        VisitArgumentList(node.ArgumentList);
                    }
                }
                else {
                    var typeParamObjCreate = oper as ITypeParameterObjectCreationExpression;
                    if (null != typeParamObjCreate) {
                        CodeBuilder.Append("newtypeparamobject(");
                        OutputType(typeParamObjCreate.Type, node, ci, "new");
                        CodeBuilder.Append(")");
                    }
                    else {
                        Log(node, "Unknown ObjectCreationExpressionSyntax !");
                    }
                }
            }
        }
        public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            CodeBuilder.Append("anonymousobject{");
            int ct = node.Initializers.Count;
            for (int i = 0; i < ct; ++i) {
                var init = node.Initializers[i];
                if (null != init && null != init.NameEquals) {
                    CodeBuilder.Append(init.NameEquals.Name);
                    CodeBuilder.AppendFormat(" {0} ", init.NameEquals.EqualsToken.Text);
                    VisitToplevelExpression(init.Expression, string.Empty);
                    if (i < ct - 1)
                        CodeBuilder.Append(", ");
                }
                else {
                    Log(node, "Unknown AnonymousObjectCreationExpressionSyntax !");
                }
            }
            CodeBuilder.Append("}");
        }
        public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            if (null == node.Initializer) {
                var oper = m_Model.GetOperationEx(node) as IArrayCreationExpression;
                ITypeSymbol etype = SymbolTable.GetElementType(oper.ElementType);
                string elementType = ClassInfo.GetFullName(etype);
                string typeKind = etype.TypeKind.ToString();
                var rankspecs = node.Type.RankSpecifiers;
                var rankspec = rankspecs[0];
                int rank = rankspec.Rank;
                if (rank >= 1) {
                    int ct = rankspec.Sizes.Count;
                    CodeBuilder.AppendFormat("newmultiarray({0}, TypeKind.{1}, ", elementType, typeKind);
                    OutputDefaultValue(etype);
                    CodeBuilder.AppendFormat(", {0}", ct);
                    for (int i = 0; i < ct; ++i) {
                        CodeBuilder.Append(", ");
                        var exp = rankspec.Sizes[i];
                        OutputExpressionSyntax(exp);
                    }
                    CodeBuilder.Append(")");
                }
                else {
                    CodeBuilder.AppendFormat("newarray({0}, TypeKind.{1})", elementType, typeKind);
                }
            }
            else {
                VisitInitializerExpression(node.Initializer);
            }
        }
        public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            VisitInitializerExpression(node.Initializer);
        }
        #endregion
    }
}