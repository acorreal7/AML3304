const express = require('express');
const cors = require('cors');
const bodyParser = require('body-parser');
require('dotenv').config();

const app = express();
const port = process.env.PORT || 5000;
const apiUrl = process.env.API_URL;
const apiKey = process.env.API_KEY;

console.log(apiUrl, apiKey);

app.use(cors());
app.use(bodyParser.json());

const conversations = {};

app.post('/chat', async (req, res) => {

    const userInput = req.body.text;
    const conversationId = req.body.conversationId;
    const conversation = conversations[ conversationId ] || [];

    try {

        // Create history context to pass to the model
        let messages = [];
        conversation.forEach((message) => {
            messages.push({ role: 'user', content: message.user });
            messages.push({ role: 'assistant', content: message.assistant });
        });

        // Add the current user input to the history
        messages.push({ role: 'user', content: userInput });

        const response = await fetch(apiUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': apiKey
            },
            body: JSON.stringify({
                messages: messages,
                frequency_penalty: 0,
                presence_penalty: 0,
                max_tokens: 512,
                seed: 42,
                stop: "<|endoftext|>",
                stream: false,
                temperature: 0,
                top_p: 1,
                response_format: {
                    "type": "text"
                }
            })
        })
        const result = await response.json();

        // Save the conversation
        conversation.push({ user: userInput, assistant: result.choices[ 0 ].message.content });
        conversations[ conversationId ] = conversation;

        // Return the response
        res.send({ text: result.choices[ 0 ].message.content });

    } catch (error) {
        res.send({ error: true, error: error });
    }


});

app.listen(port, () => {
    console.log(`App is listening on port ${port}`);
});
