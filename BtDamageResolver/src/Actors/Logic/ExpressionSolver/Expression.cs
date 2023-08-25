using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api;

namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;

/// <summary>
/// A mathematical expression.
/// </summary>
public class Expression
{
    private readonly List<Expression> _expressions;
    private readonly ExpressionFunction _outerFunction;
    private readonly IResolverRandom _random;
    private readonly List<Token> _tokens;
    private readonly decimal? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Expression"/> class.
    /// </summary>
    /// <param name="random">The random number generator.</param>
    /// <param name="input">The input text to generate from.</param>
    /// <param name="functionType">The function type to apply.</param>
    public Expression(IResolverRandom random, string input, ExpressionFunction functionType = ExpressionFunction.None)
    {
        _outerFunction = functionType;
        _random = random;
        _expressions = new List<Expression>();
        _tokens = new List<Token>();
        Construct(input);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Expression"/> class.
    /// </summary>
    /// <param name="input">The input text to generate from.</param>
    public Expression(decimal input)
    {
        _outerFunction = ExpressionFunction.None;
        _value = input;
    }

    /// <summary>
    /// Parses the expression.
    /// </summary>
    /// <returns>The decimal result of the expression.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when unrecognized tokens are encountered.</exception>
    public decimal Parse()
    {
        // Nothing to parse, finished here
        if (_value.HasValue)
        {
            return ExecuteFunction(_value.Value, _outerFunction);
        }

        while (_tokens.Count > 0)
        {
            var restart = false;

            foreach (var token in new[] { Token.Dice, Token.Exponent, Token.Divide, Token.Multiply, Token.Plus, Token.Minus })
            {
                for (int ii = 0; ii < _tokens.Count; ii++)
                {
                    if (_tokens[ii] == token)
                    {
                        var value = 0m;

                        switch (token)
                        {
                            case Token.Dice:
                                var numDice = decimal.ToInt32(_expressions[ii].Parse());
                                var diceSize = decimal.ToInt32(_expressions[ii + 1].Parse());

                                for (var diceIndex = 0; diceIndex < numDice; diceIndex++)
                                {
                                    value += _random.NextPlusOne(diceSize);
                                }

                                break;
                            case Token.Exponent:
                                value = (decimal)Math.Pow(decimal.ToDouble(_expressions[ii].Parse()), decimal.ToDouble(_expressions[ii + 1].Parse()));
                                break;
                            case Token.Divide:
                                value = _expressions[ii].Parse() / _expressions[ii + 1].Parse();
                                break;
                            case Token.Multiply:
                                value = _expressions[ii].Parse() * _expressions[ii + 1].Parse();
                                break;
                            case Token.Plus:
                                value = _expressions[ii].Parse() + _expressions[ii + 1].Parse();
                                break;
                            case Token.Minus:
                                value = _expressions[ii].Parse() - _expressions[ii + 1].Parse();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(token.ToString());
                        }

                        _expressions[ii] = new Expression(value);
                        _expressions.RemoveAt(ii + 1);
                        _tokens.RemoveAt(ii);
                        restart = true;
                        break;
                    }
                }

                if (restart)
                {
                    break;
                }
            }
        }

        return ExecuteFunction(_expressions[0].Parse(), _outerFunction);
    }

    private static (string Expression, string Remaining) ExtractSubExpression(string input)
    {
        if (input[0] != '(')
        {
            throw new ArgumentException("Invalid expressions string for subexpression builder, no opening parenthesis available.");
        }

        string subExpression = null;
        string remaining = null;

        var depth = 1;
        for (var ii = 1; ii < input.Length; ii++)
        {
            if (input[ii] == '(')
            {
                depth++;
            }

            if (input[ii] == ')')
            {
                depth--;
            }

            if (depth == 0)
            {
                subExpression = input.Substring(1, ii - 1);
                remaining = input.Substring(ii + 1);
            }
        }

        if (subExpression == null)
        {
            throw new ArgumentException("Invalid expressions string for subexpression builder, no closing parenthesis available.");
        }

        return (subExpression, remaining);
    }

    private static (string SubExpression, ExpressionFunction FunctionType, string Remaining) ExtractFunctionType(string input)
    {
        ExpressionFunction foundFunction = ExpressionFunction.None;
        string functionScope = null;

        foreach (var functionName in Enum.GetNames(typeof(ExpressionFunction)))
        {
            if (input.StartsWith(functionName))
            {
                foundFunction = Enum.Parse<ExpressionFunction>(functionName);
                var index = input.IndexOf('(');
                functionScope = input.Substring(index);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(functionScope))
        {
            throw new ArgumentException("Invalid expression string for function type extractor. No subexpression available.");
        }

        var (subExpression, remaining) = ExtractSubExpression(functionScope);

        return (subExpression, foundFunction, remaining);
    }

    private static decimal ExecuteFunction(decimal input, ExpressionFunction functionType)
    {
        switch (functionType)
        {
            case ExpressionFunction.None:
                return input;
            case ExpressionFunction.Round:
                return Math.Round(input, MidpointRounding.AwayFromZero);
            case ExpressionFunction.Ceil:
                return Math.Ceiling(input);
            case ExpressionFunction.Floor:
                return Math.Floor(input);
            default:
                throw new ArgumentOutOfRangeException(nameof(functionType), functionType, null);
        }
    }

    private void Construct(string input)
    {
        // If we start with a token, such as "-", put 0 before this so that it can be resolved properly
        if (input[0].IsToken())
        {
            _expressions.Add(new Expression(_random, "0"));
            _tokens.Add((Token)input[0]);
            input = input.Substring(1);
        }

        while (input.Length > 0)
        {
            var nothingFound = true;

            for (var ii = 0; ii < input.Length; ii++)
            {
                if (input[ii].IsToken())
                {
                    _tokens.Add((Token)input[ii]);
                    _expressions.Add(new Expression(decimal.Parse(input.Substring(0, ii))));
                    input = input.Substring(ii + 1);
                    nothingFound = false;
                    break;
                }

                if (input[ii] == '(')
                {
                    var (expression, remaining) = ExtractSubExpression(input);
                    _expressions.Add(new Expression(_random, expression));
                    input = remaining;
                    nothingFound = false;
                    break;
                }

                if (char.IsLetter(input[ii]))
                {
                    var (expression, functionType, remaining) = ExtractFunctionType(input);
                    _expressions.Add(new Expression(_random, expression, functionType));
                    input = remaining;
                    nothingFound = false;
                    break;
                }
            }

            if (nothingFound)
            {
                _expressions.Add(new Expression(decimal.Parse(input)));
                input = string.Empty;
            }
        }
    }
}