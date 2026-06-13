// using System.Linq.Expressions;

// namespace W1E1.Core
// {
//     public enum FilterNodeType 
//     { 
//         Raw, 
//         PropertyCompare, 
//         ExpressionTree, 
//         Group 
//     }
    
//     public enum LogicalOperator 
//     { 
//         And, 
//         Or 
//     }

//     internal sealed class FilterAstNode
//     {
//         public FilterNodeType NodeType { get; set; }
//         public LogicalOperator Operator { get; set; }
        
//         public string RawSql { get; set; } = string.Empty;
        
//         public Expression? LambdaExpression { get; set; }
        
//         public string PropertyName { get; set; } = string.Empty;
//         public string RelationalOperator { get; set; } = string.Empty;
//         public object? Value { get; set; }
        
//         public List<FilterAstNode>? Children { get; set; }
//     }
// }