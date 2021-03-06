﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Diogo Lucas">
//
// Copyright (C) 2010 Diogo Lucas
//
// This file is part of Moo.
//
// Moo is free software: you can redistribute it and/or modify
// it under the +terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along Moo.  If not, see http://www.gnu.org/licenses/.
// </copyright>
// <summary>
// Moo is a object-to-object multi-mapper.
// Email: diogo.lucas@gmail.com
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Moo.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;

    /// <summary>
    /// Base class for fluently setting mapping targets
    /// </summary>
    /// <typeparam name="TSource">Type of the mapping source.</typeparam>
    /// <typeparam name="TTarget">Type of the mapping target.</typeparam>
    /// <typeparam name="TInnerSource">Type of the source property/expression.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Design",
        "CA1005:AvoidExcessiveParametersOnGenericTypes",
        Justification = "I wish I could. No big deal, though, as type inference makes the specifications not necessary in client code.")]
    public class TargetSpec<TSource, TTarget, TInnerSource> : ITargetSpec<TSource, TTarget, TInnerSource>
    {
        /// <summary>The expression handler to use.</summary>
        private IExpressionHandler expressionHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetSpec{TSource,TTarget, TInnerSource}"/> class.
        /// </summary>
        /// <param name="mapper">Mapper to extend.</param>
        /// <param name="sourceArgument">Expression to pull source data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Easier said than done")]
        public TargetSpec(IExtensibleMapper<TSource, TTarget> mapper, Expression<Func<TSource, TInnerSource>> sourceArgument)
            : this(mapper, sourceArgument, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetSpec{TSource,TTarget, TInnerSource}"/>
        /// class.
        /// </summary>
        /// <param name="mapper">Mapper to extend.</param>
        /// <param name="sourceArgument">Expression to pull source data.</param>
        /// <param name="useInnerMapper">
        /// Determines whether this mapping should be carried by an internal mapper.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Easier said than done")]
        internal TargetSpec(
            IExtensibleMapper<TSource, TTarget> mapper, 
            Expression<Func<TSource, TInnerSource>> sourceArgument, 
            bool useInnerMapper)
        {
            Guard.CheckArgumentNotNull(mapper, "mapper");
            Guard.CheckArgumentNotNull(sourceArgument, "sourceArgument");
            this.Mapper = mapper;
            this.SourceArgument = sourceArgument;
            this.UseInnerMapper = useInnerMapper;
        }

        /// <summary>
        /// Gets the mapper that to be extended.
        /// </summary>
        protected IExtensibleMapper<TSource, TTarget> Mapper { get; private set; }

        /// <summary>
        /// Gets the expression that pulls source data.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Easier said than done")]
        protected Expression<Func<TSource, TInnerSource>> SourceArgument { get; private set; }

        /// <summary>Instructs Moo to map a source expression to the property expression below.</summary>
        /// <remarks>
        /// The argument parameter must be a property access expression, such as <c>(t) =&gt; t.Name</c>,
        /// or else an ArgumentException will be thrown.
        /// </remarks>
        /// <typeparam name="TInnerTarget">Type of the inner target property.</typeparam>
        /// <param name="argument">An expression fetching the property to map to.</param>
        /// <returns>A spec object allowing further fluent setup.</returns>
        public ISourceSpec<TSource, TTarget> To<TInnerTarget>(Expression<Func<TTarget, TInnerTarget>> argument)
        {
            Guard.CheckArgumentNotNull(argument, "argument");
            Guard.CheckArgumentNotNull(argument.Body, "argument.Body");
            ExpressionHandler.ValidatePropertyExpression(argument);

            if (UseInnerMapper)
            {
                ExpressionHandler.ValidatePropertyExpression(SourceArgument);
                Mapper.AddInnerMapper<TInnerSource, TInnerTarget>(
                    ExpressionHandler.GetProperty(SourceArgument),
                    ExpressionHandler.GetProperty(argument));

                return new SourceSpec<TSource, TTarget>(Mapper);
            }
            else
            {
                Mapper.AddMappingAction(
                    ExpressionHandler.GetMemberName(SourceArgument.Body),
                    ExpressionHandler.GetMemberName(argument.Body),
                    ExpressionHandler.GetAction<TSource, TTarget>(SourceArgument, argument));

                return new SourceSpec<TSource, TTarget>(Mapper);
            }
        }

        /// <summary>
        /// Combines the two mapping expressions (one to get the target, the other to produce data from
        /// the source) into a mapping action.
        /// </summary>
        /// <typeparam name="TInnerTarget">Type of the inner target property.</typeparam>
        /// <param name="sourceExpr">Expression to pull source data.</param>
        /// <param name="targetExpr">Expression to determine target property.</param>
        /// <returns>A mapping action, mapping from the source to the target expression.</returns>
        private static MappingAction<TSource, TTarget> GetAction<TInnerTarget>(
            Expression<Func<TSource, TInnerSource>> sourceExpr,
            Expression<Func<TTarget, TInnerTarget>> targetExpr)
        {
            // Decomposition of (a, b) => b.FooBar = func(a) :
            // 1. (a, 
            // 2. b) => 
            // 3. b.FooBar 
            // 4. =
            // 5. func(a) -- we still need the method call

            // 1:
            var sourceParam = Expression.Parameter(typeof(TSource));
            
            // 2:
            var targetParam = Expression.Parameter(typeof(TTarget));
            
            // 3:
            var targetPropName = ((MemberExpression)targetExpr.Body).Member.Name;
            var targetProp = Expression.Property(targetParam, targetPropName);
            
            // 5:
            var sourceGet = Expression.Convert(Expression.Invoke(sourceExpr, sourceParam), targetProp.Type);
            
            // 4:
            var assignment = Expression.Assign(targetProp, sourceGet);

            var finalExpr = Expression.Lambda<MappingAction<TSource, TTarget>>(assignment, sourceParam, targetParam);

            return finalExpr.Compile();
        }

        /// <summary>
        /// Gets the mapping key to a given expression.
        /// </summary>
        /// <param name="argument">Expression to extract the key.</param>
        /// <returns>A string containing the key to use in the mapping table.</returns>
        /// <remarks>
        /// In case of property accessors this will return the property name (important,
        /// as we want this to allow mapping overwrites for the same target property), in
        /// other cases, merely the expression.ToSting.
        /// </remarks>
        private static string GetMemberName(Expression argument)
        {
            var memberExpr = argument as System.Linq.Expressions.MemberExpression;
            if (memberExpr != null)
            {
                return memberExpr.Member.Name;
            }

            return argument.ToString();
        }

        /// <summary>Gets the expression handler to use.</summary>
        /// <value>The expression handler to use.</value>
        internal IExpressionHandler ExpressionHandler 
        {
            get
            {
                if (expressionHandler == null)
                {
                    expressionHandler = new ExpressionHandler();
                }

                return expressionHandler;
            }

            private set 
            { 
                expressionHandler = value; 
            }
        }

        /// <summary>Gets or sets a value indicating whether this mapping should be carried by an inner mapper.</summary>
        /// <value>true if use the mapping should be carried by an inner mapper, false otherwise.</value>
        public bool UseInnerMapper { get; set; }
    }
}
