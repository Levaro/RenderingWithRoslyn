using System;

namespace Levaro.CSharp.Display
{
    /// <summary>
    /// Represents the class that contains event data for the <c>SyntaxVisiting</c> event.
    /// </summary>
    /// <remarks>
    /// The event data contains a <see cref="SyntaxTreeElement"/> object which is a convenience container for either syntax nodes,
    /// tokens or trivia. An value of the <see cref="SyntaxVisitingState"/> enumeration indicates exactly from where the event
    /// was raised.
    /// </remarks>
    public class SyntaxVisitingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxVisitingEventArgs"/> class setting the properties to their
        /// "empty" values.
        /// </summary>
        public SyntaxVisitingEventArgs() : this(SyntaxTreeElement.Empty, SyntaxVisitingState.Unknown)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxVisitingEventArgs"/> class.
        /// </summary>
        /// <param name="syntaxItem">The syntax item; either a syntax node, token or trivia. The object is used to construct
        /// a <see cref="SyntaxTreeElement"/> having the underlying syntax item. If <c>syntaxItem</c> is not one of these types,
        /// the "empty" <c>SyntaxTreeElement</c> is created.</param>
        /// <param name="state">An value of the <see cref="SyntaxVisitingState"/> enumeration that describes the location 
        /// where the event is raised.</param>
        public SyntaxVisitingEventArgs(object syntaxItem, SyntaxVisitingState state) 
            : this(SyntaxTreeElement.Create(syntaxItem), state)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxVisitingEventArgs"/> class.
        /// </summary>
        /// <param name="syntaxTreeElement">The syntax tree element instance. If null, the "empty" <c>SyntaxTreeElement</c> is
        /// used.</param>
        /// <param name="state">The visiting state, that is, the location where the event is raised.</param>
        public SyntaxVisitingEventArgs(SyntaxTreeElement syntaxTreeElement, SyntaxVisitingState state)
        {
            SyntaxTreeElement = syntaxTreeElement ?? SyntaxTreeElement.Empty;
            State = state;
        }

        /// <summary>
        /// Gets the <see cref="SyntaxTreeElement"/> object.
        /// </summary>
        /// <value>
        /// The syntax tree element; it is never <c>null</c>, but can be the empty instance (that is, <c>IsEmpty</c> is 
        /// <c>true</c>).
        /// </value>
        public SyntaxTreeElement SyntaxTreeElement
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the visiting state, that is, the location of the visitor when the event is raised.
        /// </summary>
        /// <value>
        /// The state as a value of the <see cref="SyntaxVisitingState"/> enumeration.
        /// </value>
        public SyntaxVisitingState State
        {
            get;
            private set;
        }
    }
}
