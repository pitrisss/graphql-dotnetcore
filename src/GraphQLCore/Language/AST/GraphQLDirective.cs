﻿using System.Collections.Generic;

namespace GraphQLCore.Language.AST
{
    public class GraphQLDirective : ASTNode
    {
        public IEnumerable<GraphQLArgument> Arguments { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.Directive;
            }
        }

        public GraphQLName Name { get; set; }
    }
}