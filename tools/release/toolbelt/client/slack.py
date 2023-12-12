from typing import List

from toolbelt.exceptions import ResponseError

from .session import BaseUrlSession

SLACK_BASE_URL = "https://slack.com"


class SlackClient:
    def __init__(self, token: str) -> None:
        """
        SlackClient implementation with slack rest api. [https://api.slack.com/web]

        :param token: The token of the bot
        :type token: str
        """

        self._token = token
        self._session = BaseUrlSession(SLACK_BASE_URL)

        self._session.headers.update({"Authorization": f"Bearer {self._token}"})

    def send_simple_msg(self, channel: str, msg: str):
        """
        `send_simple_msg` sends a simple message to a channel

        :param channel: The channel to send the message to
        :type channel: str

        :param msg: The message to send
        :type msg: str

        :return: The response from the Slack API.
        """

        return self.send_msg(channel, text=msg)

    def send_msg(self, channel: str, *, text: str, blocks: List[dict] = []):
        """
        It sends a message to a channel

        :raises:
            ResponseError: Slack API response is not ok

        :param channel: The channel to send the message to
        :type channel: str

        :param text: The text of the message you want to send. If you want to send a message with multiple
        blocks, you can leave this blank
        :type text: str

        :param blocks: A list of blocks to be sent as part of the message
        :type blocks: List[str]

        :return: The response from the Slack API.
        """

        r = self._session.post(
            "/api/chat.postMessage",
            data={
                "channel": channel,
                "mrkdwn": True,
                "text": text,
                "blocks": blocks,
            },
        )

        r.raise_for_status()

        response = r.json()

        if not response["ok"]:
            raise ResponseError(f"SlackAPI ResponseError: body: {response}")

        return response
