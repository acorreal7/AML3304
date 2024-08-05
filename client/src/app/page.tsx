"use client";

import '@szhsin/react-menu/dist/index.css';
import MarkdownPreview from '@uiw/react-markdown-preview';
import React, { useEffect, useId, useRef, useState } from 'react';

interface IMessage {
  text: string;
  sender: 'user' | 'bot';
}

const apiUrl = 'http://localhost:5000/chat';

const Chat: React.FC = () => {

  const conversationId = useId();
  const [ messages, setMessages ] = useState<IMessage[]>([]);
  const [ state, setState ] = useState<{ userInput: string; isLoading: boolean; }>({ userInput: '', isLoading: false });
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  const handleSendMessage = async () => {

    // Check if the input value is empty
    if (state.userInput.trim() === '') return;

    // Create a new message object
    const newMessage: IMessage = { text: state.userInput, sender: 'user', };
    setState({ ...state, userInput: '', isLoading: true, });
    setMessages(prevMessages => [ ...prevMessages, newMessage ]);

    try {

      const response = await fetch(apiUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          text: state.userInput,
          conversationId: conversationId
        }),
      });

      if (!response.ok) {
        throw new Error(`Error sending message: ${response.statusText}`);
      }

      const data = await response.json();
      const botMessage: IMessage = {
        text: data.text,
        sender: 'bot',
      };

      setMessages(prevMessages => [ ...prevMessages, botMessage ]);

    } catch (error) {
      console.error('Error sending message:', error);
    } finally {
      setState({ userInput: '', isLoading: false });
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setState({ ...state, userInput: e.target.value });
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSendMessage();
    }
  };

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [ messages ]);

  return (
    <div className="chat-wrapper">
      <div className="header">
        <div className="header-right">
          <span className="username">Project Management Chat</span>
        </div>
      </div>
      <div className="main-container">
        <div className="chat-container">
          <div className="messages-container">
            {messages.length === 0 && (
              <div className="welcome-message">
                Hello, I am a project management bot. Ask me anything about project management!
              </div>
            )}
            {messages.map((message, index) => (
              <div key={index} className={`message-bubble ${message.sender}`}>
                {message.sender === 'user' && <div className="message-sender">{message.text}</div>}
                {message.sender === 'bot' && (
                  <MarkdownPreview source={message.text || ''} remarkPlugins={[]} wrapperElement={{ 'data-color-mode': 'light' }} />
                )}
              </div>
            ))}
            {state.isLoading && <div className="message-bubble bot">Generating response...</div>}
            <div ref={messagesEndRef}></div>
          </div>
          <div className="input-container">
            <input
              type="text"
              value={state.userInput}
              onChange={handleInputChange}
              onKeyPress={handleKeyPress}
              placeholder="Type a message..."
              className="input-message"
            />
            <button onClick={handleSendMessage} disabled={state.isLoading} className="send-button">
              {state.isLoading ? 'Sending...' : 'Send'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Chat;
