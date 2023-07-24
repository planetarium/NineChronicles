import { useQueries } from "@tanstack/react-query";
import axios from "axios";
import { useMemo } from "react";

interface Block { index: number }
interface Action { id: string, obsoleteAt: number }

const fetchLatestBlock = async () => {
  const response = await axios.get<Block>(`${import.meta.env.VITE_API_PATH}/api/blocks/latest`);
  return response.data;
};

const fetchActions = async () => {
  const response = await axios.get<Action[]>(import.meta.env.VITE_OBSOLETE_DATA_PATH);
  return response.data;
}

function App() {
  const [latestBlockQuery, actionsQuery] = useQueries({
    queries: [
      { queryKey: ['latestBlock'], queryFn: fetchLatestBlock },
      { queryKey: ['actions'], queryFn: fetchActions },
    ],
  });
  const isLoading = latestBlockQuery.isLoading || actionsQuery.isLoading;
  const isError = latestBlockQuery.isError || actionsQuery.isError;

  const currentBlock = useMemo(
    () => latestBlockQuery.data?.index ?? 0,
    [latestBlockQuery.data],
  );
  const actions = useMemo(
    () => actionsQuery.data?.sort((a, b) => b.obsoleteAt - a.obsoleteAt) ?? [],
    [actionsQuery.data],
  );

  if (isLoading) return <></>;
  if (isError) return <></>;

  return <></>;
}

export default App;
