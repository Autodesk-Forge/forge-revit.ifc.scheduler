import React from 'react';
import {observer} from 'mobx-react-lite';
import {useHistory, useParams} from "react-router-dom";
import {useTranslation} from "react-i18next";
import {appState} from "../App";
import {SearchableAndSortableTable} from "./SearchableAndSortableTable";
import {Project} from "../Utilities/DataTypes/Project";

export const ProjectsList = observer(()=>{
    const { t } = useTranslation();
    const history = useHistory();
    const {projectId} = useParams();

    return (
        <React.Fragment>
            <SearchableAndSortableTable
                title={t("Projects")}
                placeholder={t("Search Projects")}
                items={appState.projects}
                onItemInvoked={(item: Project)=>{
                    console.log(item);
                    history.push(`/projects/${item.id}`)}}
                columns={[{key: 'name', fieldName: 'name', minWidth: 20, name: t("Project Name")}]}
                selectedRowId={projectId}
            />
            <div style={{borderLeft: '1px solid grey'}}/>
        </React.Fragment>)
})